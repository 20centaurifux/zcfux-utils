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
using System.Collections.Concurrent;
using System.Diagnostics;

namespace zcfux.Telemetry.Discovery;

public sealed class PendingRequests
{
    sealed class Request
    {
        readonly TaskCompletionSource<object> _tcs = new();
        readonly Type _type;
        readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        readonly TimeSpan _timeToLive;
        readonly ISerializer _serializer;

        public Request(Type type, TimeSpan timeToLive, ISerializer serializer)
            => (_type, _timeToLive, _serializer) = (type, timeToLive, serializer);

        public Task<object> Task => _tcs.Task;

        public bool IsExpired => _stopwatch.Elapsed > _timeToLive;

        public void SetResult(byte[] payload)
        {
            try
            {
                var result = _serializer.Deserialize(payload, _type)
                             ?? throw new InvalidOperationException("Response cannot be null.");

                _tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        }
    }

    readonly ISerializer _serializer;
    readonly ConcurrentDictionary<string, Request> _requests = new();

    public PendingRequests(ISerializer serializer)
        => _serializer = serializer;

    public Task<object> Add(NodeDetails nodeDetails, RequestEventArgs args)
    {
        var key = ToKey(nodeDetails, args);

        var request = new Request(
            args.ResponseType,
            TimeSpan.FromSeconds(args.TimeToLive),
            _serializer);

        _requests[key] = request;

        return request.Task;
    }

    public void HandleResponseEvent(ResponseEventArgs e)
    {
        var key = ToKey(e);

        if (_requests.TryGetValue(key, out var request))
        {
            if (request.IsExpired)
            {
                _requests.Remove(key, out _);
            }
            else
            {
                request.SetResult(e.Payload);
            }
        }
    }

    static string ToKey(NodeDetails nodeDetails, RequestEventArgs args)
        => ToKey(
            nodeDetails.Domain,
            nodeDetails.Kind,
            nodeDetails.Id,
            args.MessageId);

    static string ToKey(ResponseEventArgs e)
        => ToKey(
            e.Node.Domain,
            e.Node.Kind,
            e.Node.Id,
            e.MessageId);

    static string ToKey(string domain, string kind, int nodeId, int messageId)
        => $"{domain}/{kind}/{nodeId}/{messageId}";
}