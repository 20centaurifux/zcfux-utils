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
namespace zcfux.Logging.NLog;

[Logger("nlog")]
public sealed class Writer : IWriter
{
    const string DefaultLoggerName = "Log";

    global::NLog.ILogger? _logger;

    public string Name => _logger?.Name
                          ?? throw new InvalidOperationException();

    public void Setup(string name)
        => _logger = global::NLog.LogManager.GetLogger(name ?? DefaultLoggerName);

    public void WriteMessage(ESeverity severity, string message)
        => _logger!.Log(Mapper.Map(severity), message);

    public void WriteException(ESeverity severity, Exception exception)
        => _logger!.Log(Mapper.Map(severity), exception);
}