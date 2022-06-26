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
using Microsoft.Data.Sqlite;

namespace zcfux.KeyValueStore.Persistent;

internal sealed class Db : IDisposable
{
    internal sealed record Association(string Key, string Hash, byte[]? Blob);

    const int Revision = 1;

    readonly string _connectionString;
    readonly object _writerLock = new();
    readonly SqliteConnection _writerConnection;
    readonly LinqToDBConnectionOptions _linqToDbConnection;

    public Db(string directory)
    {
        _connectionString = BuildConnectionString(directory);

        _writerConnection = new SqliteConnection(_connectionString);

        _linqToDbConnection = BuildLinqToDBConnectionOptions(_connectionString);
    }

    static LinqToDBConnectionOptions BuildLinqToDBConnectionOptions(string connectionString)
    {
        var builder = new LinqToDBConnectionOptionsBuilder();

        builder.UseSQLite(connectionString);

        var opts = builder.Build();

        return opts;
    }

    internal static string BuildConnectionString(string directory)
    {
        var builder = new SqliteConnectionStringBuilder();

        var path = Path.Combine(directory, "index.db");

        builder.DataSource = path;

        return builder.ConnectionString;
    }

    public void Setup()
    {
        lock (_writerLock)
        {
            _writerConnection.Open();

            var revision = GetRevision_Unlocked();

            if (revision > Revision)
            {
                throw new StorageException("Index version not supported.");
            }

            if (revision < Revision)
            {
                UpgradeSchema_Unlocked();
            }
        }
    }

    int GetRevision_Unlocked()
    {
        var revision = 0;

        if (RevisionTableExists_Unlocked())
        {
            revision = FetchRevision_Unlocked();
        }

        return revision;
    }

    bool RevisionTableExists_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='Revision'";

            var result = cmd.ExecuteScalar();

            return Convert.ToInt32(result) == 1;
        }
    }

    int FetchRevision_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT Version FROM Revision";

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    void UpgradeSchema_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE Revision (Version INT NOT NULL)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO Revision (Version) VALUES (1)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE Association (
                                  Key VARCHAR(512) NOT NULL,
                                  Hash CHAR(64) NOT NULL,
                                  PRIMARY KEY (Key))";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE TABLE Blob (
                                  Hash CHAR(64) NOT NULL,
                                  Length INT NOT NULL,
                                  Contents BLOB,
                                  PRIMARY KEY (Hash))";

            cmd.ExecuteNonQuery();
        }
    }

    public IEnumerable<string> GetKeys()
        => new DataContext(_linqToDbConnection)
            .GetTable<AssociationRelation>()
            .Select(assoc => assoc.Key);

    public void Put(string key, string hash, byte[] contents)
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = "INSERT OR IGNORE INTO Blob (Hash, Length, Contents) VALUES (@hash, @length, @contents)";

                cmd.Parameters.AddWithValue("@hash", hash);
                cmd.Parameters.AddWithValue("@length", contents.Length);
                cmd.Parameters.AddWithValue("@contents", contents);

                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();

                cmd.CommandText = "REPLACE INTO Association (Key, Hash) VALUES (@key, @hash)";

                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@hash", hash);

                cmd.ExecuteNonQuery();
            }
        }
    }

    public void Associate(string key, string hash)
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = "REPLACE INTO Association (Key, Hash) VALUES (@key, @hash)";

                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@hash", hash);

                cmd.ExecuteNonQuery();
            }
        }
    }

    public Association Fetch(string key)
    {
        using (var readerConnection = new SqliteConnection(_connectionString))
        {
            readerConnection.Open();

            using (var cmd = readerConnection.CreateCommand())
            {
                cmd.CommandText = @"SELECT Association.Hash, Blob.Length, Blob.Contents
                                      FROM Association
                                      LEFT JOIN Blob ON Blob.Hash=Association.Hash
                                      WHERE Key=@key";

                cmd.Parameters.AddWithValue("@key", key);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new KeyNotFoundException(key);
                    }

                    var hash = reader.GetString(0);

                    byte[]? contents = null;

                    if (!reader.IsDBNull(2))
                    {
                        var length = reader.GetInt32(1);

                        contents = new byte[length];

                        reader.GetBytes(2, 0, contents, 0, length);
                    }

                    return new Association(key, hash, contents);
                }
            }
        }
    }

    public IEnumerable<(string, byte[])> FetchSmallBlobs()
    {
        using (var readerConnection = new SqliteConnection(_connectionString))
        {
            readerConnection.Open();

            using (var cmd = readerConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT Hash, Length, Contents FROM Blob WHERE Length>0";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var hash = reader.GetString(0);
                        var length = reader.GetInt32(1);

                        var bytes = new byte[length];

                        reader.GetBytes(2, 0, bytes, 0, length);

                        yield return (hash, bytes);
                    }
                }
            }
        }
    }

    public IEnumerable<string> FetchLargeBlobs()
    {
        using (var readerConnection = new SqliteConnection(_connectionString))
        {
            readerConnection.Open();

            using (var cmd = readerConnection.CreateCommand())
            {
                cmd.CommandText = @"SELECT Hash FROM Association
                                      WHERE NOT EXISTS (SELECT Hash FROM Blob WHERE Blob.Hash=Association.Hash)";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }
    }

    public void Remove(string key)
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Association WHERE Key=@key";

                cmd.Parameters.AddWithValue("@key", key);

                cmd.ExecuteNonQuery();
            }
        }
    }

    public int RemoveBlobAssociations(string hash)
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Association WHERE Hash=@hash";

                cmd.Parameters.AddWithValue("@hash", hash);

                return cmd.ExecuteNonQuery();
            }
        }
    }

    public bool RemoveSmallBlob(string hash)
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM Blob WHERE Hash=@hash";

                cmd.Parameters.AddWithValue("@hash", hash);

                return Convert.ToInt32(cmd.ExecuteNonQuery()) > 0;
            }
        }
    }

    public bool IsOrphan(string hex)
    {
        using (var readerConnection = new SqliteConnection(_connectionString))
        {
            readerConnection.Open();

            using (var cmd = readerConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(Key) FROM Association WHERE Hash=@hash";

                cmd.Parameters.AddWithValue("@hash", hex);

                var result = cmd.ExecuteScalar();

                return Convert.ToInt32(result) == 0;
            }
        }
    }

    public void RemoveOrphans()
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = @"DELETE FROM Blob
                                      WHERE NOT EXISTS (SELECT * FROM Association WHERE Hash=Blob.Hash)";

                cmd.ExecuteNonQuery();
            }
        }
    }

    public void Vacuum()
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = "VACUUM";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void Dispose()
        => _writerConnection.Dispose();
}