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
using System.Diagnostics;
using System.Reflection;

namespace zcfux.Logging.NLog;

[Logger("nlog")]
public sealed class Writer : IWriter
{
    const int MaxFrames = 100;

    readonly global::NLog.ILogger _logger;

    public Writer()
    {
        var callingAssembly = GetCallingAssembly();

        _logger = global::NLog.LogManager.GetLogger(callingAssembly.GetName().Name);
    }

    static Assembly GetCallingAssembly()
    {
        Assembly executingAssembly = Assembly.GetExecutingAssembly();

        for (var skip = 0; skip < MaxFrames; ++skip)
        {
            var assembly = new StackFrame(skipFrames: skip, needFileInfo: false)
                .GetMethod()
                ?.DeclaringType
                ?.Assembly;

            if (assembly != null
                && assembly.FullName != executingAssembly.FullName)
            {
                return assembly;
            }
        }

        return executingAssembly;
    }

    public void WriteMessage(ESeverity severity, string message)
        => _logger.Log(Mapper.Map(severity), message);

    public void WriteException(ESeverity severity, Exception exception)
        => _logger.Log(Mapper.Map(severity), exception);
}