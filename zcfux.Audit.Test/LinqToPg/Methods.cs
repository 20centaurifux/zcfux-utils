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
using LinqToDB;
using LinqToDB.Configuration;
using zcfux.Data;
using zcfux.Data.LinqToDB;

namespace zcfux.Audit.Test.LinqToPg;

internal class Methods : IDbTestMethods
{
    const string DefaultConnectionString
        = "User ID=test;Host=localhost;Port=5432;Database=test;";

    public IEngine NewEngine()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_TEST_CONNECTIONSTRING")
                               ?? DefaultConnectionString;

        var builder = new LinqToDBConnectionOptionsBuilder();

        builder.UsePostgreSQL(connectionString);

        var opts = builder.Build();

        return new Engine(opts);
    }

    public void DeleteAll(object handle)
    {
        var db = (handle as Handle)!.Db();

        db.GetTable<zcfux.Audit.LinqToPg.EventRelation>().Delete();
        db.GetTable<zcfux.Audit.LinqToPg.TopicAssociationRelation>().Delete();
        db.GetTable<zcfux.Audit.LinqToPg.TopicRelation>().Delete();
        db.GetTable<zcfux.Audit.LinqToPg.TopicKindRelation>().Delete();
        db.GetTable<zcfux.Audit.LinqToPg.AssociationRelation>().Delete();
        db.GetTable<zcfux.Audit.LinqToPg.EventRelation>().Delete();
        db.GetTable<zcfux.Audit.LinqToPg.EventKindRelation>().Delete();
    }

    public IAuditDb CreateDb()
        => new Audit.LinqToPg.AuditDb();
}