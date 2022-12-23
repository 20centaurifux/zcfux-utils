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
                    CREATE TABLE TextCategory
                    (
                        Id integer NOT NULL PRIMARY KEY,
                        Name varchar(30) NOT NULL,
                        UNIQUE(Name)
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE TextResource
                    (
                        Id integer PRIMARY KEY AUTOINCREMENT,
                        CategoryId integer NOT NULL,
                        MsgId varchar(255) NOT NULL,
                        UNIQUE(CategoryId, MsgId),
                        FOREIGN KEY(CategoryId) REFERENCES TextCategory(Id) ON DELETE CASCADE
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE Locale
                    (
                        Id integer NOT NULL PRIMARY KEY,
                        Name varchar(30) NOT NULL,
                        UNIQUE(Name)
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE TABLE TranslatedText
                    (
	                    ResourceId integer NOT NULL,
                        LocaleId integer NOT NULL,
	                    Translation varchar(255) NOT NULL,
	                    FOREIGN KEY(ResourceId) REFERENCES TextResource(Id)  ON DELETE CASCADE,
                        FOREIGN KEY(LocaleId) REFERENCES Locale(Id)  ON DELETE CASCADE,
                        PRIMARY KEY (ResourceId, LocaleId)
                    )";

                cmd.ExecuteNonQuery();

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
                        TextId integer NOT NULL,
                        Translatable integer NOT NULL,
                        FOREIGN KEY(KindId) REFERENCES TopicKind(Id),
                        FOREIGN KEY(TextId) REFERENCES TextResource(Id)
                    )";

                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
                    CREATE VIEW TopicView AS
                        SELECT
                          t.Id,
                          k.Id AS KindId,
                          k.Name AS Kind,
                          tr.Id AS TextId,
                          tr.MsgId AS MsgId,
                          tr.CategoryId AS TextCategoryId,
                          c.Name AS TextCategory,
                          t.Translatable
                        FROM
                          Topic t
                          JOIN TopicKind k ON k.Id = t.KindId
                          JOIN TextResource tr ON tr.Id = TextId
                          JOIN TextCategory c ON c.Id = CategoryId";

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
                        SELECT
                          e.Id,
                          e.KindId,
                          k.Name AS Kind,
                          e.Archived,
                          e.CreatedAt,
                          e.Severity,
                          e.TopicId,
                          COALESCE(trans.Translation, res.MsgId) AS DisplayName,
                          trans.LocaleId AS LocaleId,
                          tk.Id AS TopicKindId,
                          tk.Name AS TopicKind
                        FROM
                          Event e
                          JOIN EventKind k ON k.Id = e.KindId
                          LEFT JOIN Topic t ON t.Id = e.TopicId
                          LEFT JOIN TopicKind tk ON t.KindId = tk.Id
                          LEFT JOIN TextResource res ON res.Id = TextId
                          LEFT JOIN TranslatedText trans ON t.Translatable
                          AND trans.ResourceId = TextId";

                cmd.ExecuteNonQuery();
            }
        }
    }
}