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
namespace zcfux.Logging.Test.Writer;

[Logger("collect")]
public sealed class Collect : IWriter
{
    public static readonly IDictionary<ESeverity, IList<string>> Messages = new Dictionary<ESeverity, IList<string>>();

    public void WriteMessage(ESeverity severity, string message)
        => Write(severity, message);

    public void WriteException(ESeverity severity, Exception exception)
        => Write(severity, exception.ToString());

    static void Write(ESeverity severity, string message)
    {
        if (!Messages.TryGetValue(severity, out var messages))
        {
            messages = new List<string>();

            Messages[severity] = messages;
        }

        messages.Add(message);
    }
}