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
using Microsoft.Data.Sqlite;

namespace zcfux.Audit.Test.LinqToDB;

sealed class TestDb
{
    public string? ConnectionString { get; private set; }

    public void Create()
    {
        var path = BuildSqliteTestDbPath();

        File.Delete(path);

        ConnectionString = BuildSqLiteConnectionString(path);

        CreateDatabase(ConnectionString);
    }


    static string BuildSqliteTestDbPath()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "zcfux.Audit.LinqToDB.SQLite.db");

        return path;
    }

    static string BuildSqLiteConnectionString(string path)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Pooling = false,
            ForeignKeys = true
        };

        return builder.ConnectionString;
    }

    static void CreateDatabase(string connectionString)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE TopicKind
                    (
                        Id integer NOT NULL PRIMARY KEY,
                        Name varchar(30) NOT NULL
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE Topic
                    (
                        Id integer PRIMARY KEY AUTOINCREMENT,
                        KindId integer NOT NULL,
                        DisplayName varchar(50) NOT NULL,
                        FOREIGN KEY(KindId) REFERENCES TopicKind(Id)
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE VIEW TopicView AS
                        SELECT t.Id, k.Id AS KindId, k.Name AS Kind, t.DisplayName
                        FROM Topic t
                        JOIN TopicKind k ON k.Id = t.KindId";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE Association
                    (
                        Id integer NOT NULL PRIMARY KEY,
                        Name varchar(30) NOT NULL
                    );";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE TopicAssociation
                    (
	                    Id integer PRIMARY KEY AUTOINCREMENT,
	                    Topic1 integer NOT NULL,
	                    AssociationId integer NOT NULL,
	                    Topic2 integer NOT NULL,
	                    FOREIGN KEY(Topic1) REFERENCES Topic(Id),
	                    FOREIGN KEY(AssociationId) REFERENCES Association(Id)
	                    FOREIGN KEY(Topic2) REFERENCES Topic(Id)
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE EventKind
                    (
                        Id integer PRIMARY KEY,
                        Name varchar(30) NOT NULL
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE Event
                    (
	                    Id integer PRIMARY KEY AUTOINCREMENT,
	                    KindId integer NOT NULL,
	                    Severity integer NOT NULL,
	                    Archived integer NOT NULL,
	                    CreatedAt datetime  NOT NULL,
	                    TopicId integer,
	                    FOREIGN KEY(KindId) REFERENCES EventKind(Id),
	                    FOREIGN KEY(TopicId) REFERENCES Topic(Id)
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE VIEW EventView AS
                        SELECT e.Id, e.KindId, k.Name AS Kind, e.Archived, e.CreatedAt, e.Severity, e.TopicId,
                            t.DisplayName, tk.Id AS TopicKindId, tk.Name AS TopicKind 
                        FROM Event e
                        JOIN EventKind k ON k.Id = e.KindId
                        LEFT JOIN Topic t ON t.Id = e.TopicId
                        LEFT JOIN TopicKind tk ON t.KindId = tk.Id";

                cmd.ExecuteNonQuery();
            }
        }
    }

    static void CreateTables(SqliteConnection connection)
    {
    }
}