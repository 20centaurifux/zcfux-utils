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
using System.Collections.Immutable;

namespace zcfux.Logging;

sealed class Chain : ILogger
{
    ImmutableList<IWriter> _writers = ImmutableList<IWriter>.Empty;
    long _verbosity = 1;

    public ESeverity Verbosity
    {
        get => (ESeverity)Interlocked.Read(ref _verbosity);

        set => Interlocked.Exchange(ref _verbosity, (long)value);
    }

    public Chain Append(IWriter writer)
    {
        var chain = new Chain
        {
            Verbosity = Verbosity,
            _writers = _writers.Add(writer)
        };

        return chain;
    }

    public void Trace(string message)
        => WriteMessage(ESeverity.Trace, message);

    public void Trace(string format, params object[] args)
        => WriteMessage(ESeverity.Trace, format, args);

    public void Trace(Exception ex)
        => WriteException(ESeverity.Trace, ex);

    public void Debug(string message)
        => WriteMessage(ESeverity.Debug, message);

    public void Debug(string format, params object[] args)
        => WriteMessage(ESeverity.Debug, format, args);

    public void Debug(Exception ex)
        => WriteException(ESeverity.Debug, ex);

    public void Info(string message)
        => WriteMessage(ESeverity.Info, message);

    public void Info(string format, params object[] args)
        => WriteMessage(ESeverity.Info, format, args);

    public void Info(Exception ex)
        => WriteException(ESeverity.Info, ex);

    public void Warn(string message)
        => WriteMessage(ESeverity.Warn, message);

    public void Warn(string format, params object[] args)
        => WriteMessage(ESeverity.Warn, format, args);

    public void Warn(Exception ex)
        => WriteException(ESeverity.Warn, ex);

    public void Error(string message)
        => WriteMessage(ESeverity.Error, message);

    public void Error(string format, params object[] args)
        => WriteMessage(ESeverity.Error, format, args);

    public void Error(Exception ex)
        => WriteException(ESeverity.Error, ex);

    public void Fatal(string message)
        => WriteMessage(ESeverity.Fatal, message);

    public void Fatal(string format, params object[] args)
        => WriteMessage(ESeverity.Fatal, format, args);

    public void Fatal(Exception ex)
        => WriteException(ESeverity.Fatal, ex);

    void WriteMessage(ESeverity severity, string format, params object[] args)
    {
        foreach (var writer in GetWriters(severity))
        {
            try
            {
                var msg = string.Format(format, args);

                writer.WriteMessage(severity, msg);
            }
            catch { }
        }
    }

    void WriteException(ESeverity severity, Exception exception)
    {
        foreach (var writer in GetWriters(severity))
        {
            try
            {
                writer.WriteException(severity, exception);
            }
            catch { }
        }
    }

    IEnumerable<IWriter> GetWriters(ESeverity severity)
        => (severity >= Verbosity)
            ? _writers
            : Enumerable.Empty<IWriter>();
}