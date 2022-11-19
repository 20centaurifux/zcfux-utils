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
using LinqToDB.Configuration;
using LinqToDB.Data;

namespace zcfux.Data.LinqToDB;

public class Engine : IEngine
{
    public Engine(LinqToDBConnectionOptions options)
        => Options = options;

    protected LinqToDBConnectionOptions Options { get; }

    public virtual void Setup()
    {
        DataConnection.TurnTraceSwitchOn();

        DataConnection.WriteTraceLine = (s1, s2, l) =>
        {
            Console.WriteLine(s1, s2);
        };
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