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

namespace zcfux.Translation.Test.LinqToDB;

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
            "zcfux.Translation.LinqToDB.SQLite.db");

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
            }
        }
    }
}