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
using Npgsql;

namespace zcfux.Data.Postgres;

public sealed class Handle
{
    readonly NpgsqlConnection _connection;
    NpgsqlTransaction? _transaction;

    public Handle(NpgsqlConnection connection)
        => _connection = connection;

    public NpgsqlCommand CreateCommand(int commandTimeout = 180)
    {
        _transaction ??= _connection.BeginTransaction();

        var command = new NpgsqlCommand(string.Empty, _connection)
        {
            Transaction = _transaction,
            CommandTimeout = commandTimeout
        };


        return command;
    }

    public void CommitAndClose()
    {
        _transaction?.Commit();

        Close();
    }

    public void RollbackAndClose()
    {
        _transaction?.Rollback();

        Close();
    }

    void Close()
        => _connection.Close();
}