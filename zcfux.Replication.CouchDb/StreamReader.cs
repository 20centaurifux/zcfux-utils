/***************************************************************************
    begin........: December 2021
    copyright....: Sebastian Fedrau
    email........: sebastian.fedrau@gmail.com
 ***************************************************************************/

/***************************************************************************
    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 3 of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using MyCouch;
using MyCouch.Requests;
using MyCouch.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using zcfux.Replication.Generic;

namespace zcfux.Replication.CouchDb;

public sealed class StreamReader : AStreamReader
{
    public override event EventHandler? Started;
    public override event EventHandler? Stopped;
    public override event EventHandler<StreamReaderEventArgs>? Read;
    public override event EventHandler<StreamReaderEventArgs>? Conflict;
    public override event ErrorEventHandler? Error;

    readonly StreamReaderOptions _options;

    const long Idle = 0;
    const long Running = 1;
    const long Stopping = 2;

    long _state = Idle;

    IMyCouchClient? _client;
    CancellationTokenSource? _source;
    Task<ContinuousChangesResponse>? _task;

    public StreamReader(string side, StreamReaderOptions opts)
        : base(side)
        => _options = opts;

    public override void Start(string? since)
    {
        if (Interlocked.CompareExchange(ref _state, Running, Idle) == Idle)
        {
            try
            {
                _client = Pool.Clients.TakeOrCreate(new Uri($"{_options.Url}{Side}"));

                var req = new GetChangesRequest
                {
                    Feed = ChangesFeed.Continuous,
                    Heartbeat = Convert.ToInt32(_options.Heartbeat.TotalMilliseconds)
                };

                if (!string.IsNullOrEmpty(since))
                {
                    req.Since = since;
                }

                _source = new CancellationTokenSource();

                _task = _client.Changes.GetAsync(req, change =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(change))
                        {
                            HandleEvent(JsonConvert.DeserializeObject<Change>(change));
                        }
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke(this, new ErrorEventArgs(ex));

                        Stop();
                    }
                }, _source.Token);

                Started?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                Interlocked.Exchange(ref _state, Idle);

                throw;
            }
        }
    }

    public override void Stop()
    {
        if (Interlocked.CompareExchange(ref _state, Stopping, Running) == Running)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _source?.Cancel();
            }
            catch
            {
                // everything...
            }

            try
            {
                _task?.Wait();
            }
            catch
            {
                // ...in its right place
            }

            _client?.Dispose();

            Interlocked.Exchange(ref _state, Idle);

            Stopped?.Invoke(this, EventArgs.Empty);
        }
    }

    void HandleEvent(Change? ev)
    {
        if (ev != null && !ev.Deleted)
        {
            var response = ReceiveDocument(ev);

            var doc = JsonConvert.DeserializeObject<Document<JObject>>(response.Content);

            var version = BuildVersion(doc!, response.Rev);

            if (version != null)
            {
                var args = new StreamReaderEventArgs(ev.Seq, version);

                if (response.Conflicts == null || response.Conflicts.Length == 0)
                {
                    Read?.Invoke(this, args);
                }
                else
                {
                    Conflict?.Invoke(this, args);
                }
            }
        }
    }

    DocumentResponse ReceiveDocument(Change ev)
    {
        var rev = ev.Deleted
            ? ev.Changes[0]["rev"].ToString()
            : null;

        var req = new GetDocumentRequest(ev.Id, rev)
        {
            Conflicts = true
        };

        var response = _client!.Documents.GetAsync(req).Result;

        if (!response.IsSuccess)
        {
            throw new Exception(response.Reason);
        }

        return response;
    }

    IVersion? BuildVersion(Document<JObject> doc, string revision)
    {
        IVersion? version = null;

        if (!string.IsNullOrEmpty(doc.Kind))
        {
            var type = TypeMap.Get(doc.Kind);

            var entity = doc.Entity.ToObject(type) as IEntity;

            version = new Version<IEntity>(entity!, revision, doc.Side, doc.Modified, doc.Deleted);
        }

        return version;
    }
}