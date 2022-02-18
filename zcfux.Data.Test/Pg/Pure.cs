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
using zcfux.Data.Postgres;
using zcfux.SqlMapper;

namespace zcfux.Data.Test.Pg;

internal sealed class Pure : IPure
{
    void IPure.DeleteAll(object handle)
        => DeleteAll((handle as Handle)!);

    public void DeleteAll(Handle handle)
    {
        using (var cmd = handle.CreateCommand())
        {
            cmd.CommandText = @"DELETE FROM ""Model""";

            cmd.ExecuteNonQuery();
        }
    }

    Model IPure.New(object handle, string value)
        => New((handle as Handle)!, value);

    public Model New(Handle handle, string value)
    {
        using (var cmd = handle.CreateCommand())
        {
            var model = new Model()
            {
                Value = value
            };

            cmd.CommandText = @"INSERT INTO ""Model"" (""Value"") VALUES (@Value) RETURNING ""ID""";

            cmd.Assign(model);

            model.ID = Convert.ToInt32(cmd.ExecuteScalar());

            return model;
        }
    }

    IEnumerable<Model> IPure.All(object handle)
        => All((handle as Handle)!);

    public IEnumerable<Model> All(Handle handle)
    {
        using (var cmd = handle.CreateCommand())
        {
            cmd.CommandText = @"SELECT * FROM ""Model""";

            return cmd.FetchLazy<Model>();
        }
    }
}