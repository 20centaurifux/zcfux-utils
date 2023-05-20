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
using LinqToDB;
using LinqToDB.Data;

namespace zcfux.Data.LinqToDB;

public class Engine : IEngine
{
#pragma warning disable CS0414
    static long _initialized = 0;
#pragma warning restore CS0414

    public Engine(DataOptions options)
        => Options = options;

    protected DataOptions Options { get; }

    public virtual void Setup()
    {
#if DEBUG
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
        {
            DataConnection.TurnTraceSwitchOn();

            DataConnection.WriteTraceLine = (message, category, level) =>
            {
                Debug.Write($"{category}: {message}");
            };
        }
#endif
    }

    public Transaction NewTransaction()
        => new(NewSession());

    Session NewSession()
    {
        var conn = new DataConnection(Options);

        conn.InlineParameters = false;

        PrepareDataConnection(conn);

        var session = new Session(conn);

        return session;
    }

    protected virtual void PrepareDataConnection(DataConnection connection)
    {
    }
}