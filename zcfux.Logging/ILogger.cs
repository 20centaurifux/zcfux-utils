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
namespace zcfux.Logging;

public interface ILogger
{
    ESeverity Verbosity { get; set; }

    void Trace(string message);

    void Trace(string format, params object[] args);

    void Trace(Exception ex);

    void Debug(string message);

    void Debug(string format, params object[] args);

    void Debug(Exception ex);

    void Info(string message);

    void Info(string format, params object[] args);

    void Info(Exception ex);

    void Warn(string message);

    void Warn(string format, params object[] args);

    void Warn(Exception ex);

    void Error(string message);

    void Error(string format, params object[] args);

    void Error(Exception ex);

    void Fatal(string message);

    void Fatal(string format, params object[] args);

    void Fatal(Exception ex);
}