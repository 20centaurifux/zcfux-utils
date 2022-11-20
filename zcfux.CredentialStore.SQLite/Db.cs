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
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using zcfux.Byte;
using zcfux.Security;

namespace zcfux.CredentialStore.SQLite;

sealed class Db : IDisposable
{
    const int Revision = 1;

    readonly string _connectionString;
    readonly object _writerLock = new();
    readonly SqliteConnection _writerConnection;
    readonly byte[] _secret;
    readonly Aes _writerAes;

    public Db(string filename, string password)
    {
        _connectionString = BuildConnectionString(filename);

        _writerConnection = new SqliteConnection(_connectionString);

        _secret = PasswordToSecret(password);

        _writerAes = CreateAes();
    }

    static string BuildConnectionString(string filename)
    {
        var builder = new SqliteConnectionStringBuilder();

        var path = Path.Combine(filename);

        builder.DataSource = path;
        builder.ForeignKeys = true;

        return builder.ConnectionString;
    }

    static byte[] PasswordToSecret(string password)
    {
        var sha256 = Factory.CreateHashAlgorithm("SHA-256");

        var passwordBytes = password.GetBytes();

        return sha256.ComputeHash(passwordBytes);
    }

    Aes CreateAes()
    {
        var aes = Aes.Create();

        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.FeedbackSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Key = _secret;

        return aes;
    }

    public void Setup()
    {
        lock (_writerLock)
        {
            _writerConnection.Open();

            var revision = GetRevision_Unlocked();

            if (revision > Revision)
            {
                throw new Exception("Database version not supported.");
            }

            if (revision > 0)
            {
                if (!CheckSignature_Unlocked())
                {
                    throw new InvalidPasswordException();
                }

                CollectGarbage_Unlocked();
                Vacuum_Unlocked();
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

        if (ApplicationTableExists_Unlocked())
        {
            revision = FetchRevision_Unlocked();
        }

        return revision;
    }

    bool ApplicationTableExists_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(name) FROM sqlite_master WHERE type='table' AND name='Application'";

            var result = cmd.ExecuteScalar();

            return Convert.ToInt32(result) == 1;
        }
    }

    int FetchRevision_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT Value FROM Application WHERE Key='Revision'";

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }

    void UpgradeSchema_Unlocked()
    {
        CreateApplicationTable_Unlocked();
        CreateSecretTable_Unlocked();
        CreatePairTable_Unlocked();
    }

    void CreateApplicationTable_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = @"CREATE TABLE Application (
                                    Key VARCHAR(32) PRIMARY KEY,
                                    Value VARCHAR(64) NOT NULL)";

            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO Application (Key, Value) VALUES ('Revision', '1')";
            cmd.ExecuteNonQuery();

            var (guid, signature) = CreateSignature();

            cmd.CommandText = "INSERT INTO Application (Key, Value) VALUES ('Guid', @guid)";

            cmd.Parameters.AddWithValue("@guid", guid.ToString());

            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();

            cmd.CommandText = "INSERT INTO Application (Key, Value) VALUES ('Signature', @signature)";

            cmd.Parameters.AddWithValue("@signature", signature.ToHex());

            cmd.ExecuteNonQuery();
        }
    }

    void CreateSecretTable_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = @"CREATE TABLE Secret (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Path VARCHAR(512) NOT NULL,
                                    ExpiryDate INTEGER)";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"CREATE UNIQUE INDEX UniqueSecretPath ON Secret (Path)";
            cmd.ExecuteNonQuery();
        }
    }

    void CreatePairTable_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = @"CREATE TABLE Pair (
                                      SecretId INTEGER NOT NULL,
                                      Key VARCHAR(64) NOT NULL,
                                      IV CHAR(32) NOT NULL,
                                      Value VARCHAR(4096) NOT NULL,
                                      PRIMARY KEY (SecretId, Key),
                                      FOREIGN KEY (SecretId) REFERENCES Secret (Id) ON DELETE CASCADE)";

            cmd.ExecuteNonQuery();
        }
    }

    (Guid, byte[]) CreateSignature()
    {
        var guid = Guid.NewGuid();

        var hmac = Factory.CreateKeyedHashAlgorithm("HMAC-SHA-256", _secret);

        var hash = hmac.ComputeHash(guid.ToByteArray());

        return (guid, hash);
    }

    bool CheckSignature_Unlocked()
    {
        var success = false;

        using (var cmd = _writerConnection.CreateCommand())
        {
            Guid? guid = null;
            byte[]? signature = null;

            cmd.CommandText = @"SELECT Key, Value FROM Application WHERE Key IN ('Guid', 'Signature')";

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString(0) == "Guid")
                    {
                        guid = Guid.Parse(reader.GetString(1));
                    }
                    else
                    {
                        signature = reader.GetString(1).FromHex();
                    }
                }

                if (guid.HasValue && signature != null)
                {
                    var hmac = Factory.CreateKeyedHashAlgorithm("HMAC-SHA-256", _secret);

                    var hash = hmac.ComputeHash(guid.Value.ToByteArray());

                    success = hash.SequenceEqual(signature);
                }
            }
        }

        return success;
    }

    public void Write(string path, Secret secret)
    {
        lock (_writerLock)
        {
            var secretId = TryFindSecret_Unlocked(path);

            if (!secretId.HasValue)
            {
                secretId = CreateSecret_Unlocked(path, secret.ExpiryDate);
            }

            WritePairs_Unlocked(secretId!.Value, secret.Data);
        }
    }

    int? TryFindSecret_Unlocked(string path)
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id FROM Secret WHERE Path=@path";

            cmd.Parameters.AddWithValue("@path", path);

            var id = cmd.ExecuteScalar();

            return (id == null)
                ? null
                : Convert.ToInt32(id);
        }
    }

    int? CreateSecret_Unlocked(string path, DateTime? expiryDate)
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO Secret (Path, ExpiryDate) VALUES (@path, @expiryDate)";

            cmd.Parameters.AddWithValue("@path", path);

            if (expiryDate.HasValue)
            {
                cmd.Parameters.AddWithValue("@expiryDate", expiryDate.Value.Ticks);
            }
            else
            {
                cmd.Parameters.AddWithValue("@expiryDate", DBNull.Value);
            }

            cmd.ExecuteNonQuery();

            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT LAST_INSERT_ROWID()";

            var id = Convert.ToInt32(cmd.ExecuteScalar());

            return id;
        }
    }

    void WritePairs_Unlocked(int secretId, IReadOnlyDictionary<string, string> data)
    {
        var existingKeys = GetExistingKeys_Unlocked(secretId)
            .ToArray();

        foreach (var kv in data)
        {
            ReplacePair_Unlocked(secretId, kv.Key, kv.Value);
        }

        var removedKeys = existingKeys.Except(data.Keys)
            .ToArray();

        if (removedKeys.Any())
        {
            RemovePairs_Unlocked(secretId, removedKeys);
        }
    }

    IEnumerable<string> GetExistingKeys_Unlocked(int secretId)
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "SELECT Key FROM Pair WHERE SecretId=@secretId";

            cmd.Parameters.AddWithValue("@secretId", secretId);

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    yield return reader.GetString(0);
                }
            }
        }
    }

    void ReplacePair_Unlocked(int secretId, string key, string value)
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            _writerAes.GenerateIV();

            using (var encryptor = _writerAes.CreateEncryptor())
            {
                var ms = new MemoryStream();

                using (var stream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    stream.Write(value.GetBytes());
                }

                var iv = _writerAes.IV
                    .ToHex();

                var encryptedValue = ms
                    .ToArray()
                    .ToHex();

                cmd.CommandText = "REPLACE INTO Pair (SecretId, Key, IV, Value) VALUES (@secretId, @key, @iv, @value)";

                cmd.Parameters.AddWithValue("@secretId", secretId);
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@iv", iv);
                cmd.Parameters.AddWithValue("@value", encryptedValue);

                cmd.ExecuteNonQuery();
            }
        }
    }

    void RemovePairs_Unlocked(int secretId, string[] keys)
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM Pair WHERE SecretId=@secretId AND Key=@key";

            foreach (var key in keys)
            {
                cmd.Parameters.AddWithValue("@secretId", secretId);
                cmd.Parameters.AddWithValue("@key", key);

                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();
            }
        }
    }

    public bool Remove(string path)
    {
        lock (_writerLock)
        {
            using (var cmd = _writerConnection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(Id) FROM Secret WHERE Path=@path";

                cmd.Parameters.AddWithValue("@path", path);

                var result = cmd.ExecuteScalar();

                var deleted = Convert.ToBoolean(result);

                if (Convert.ToBoolean(result))
                {
                    cmd.CommandText = "DELETE FROM Secret WHERE Path=@path";

                    cmd.ExecuteNonQuery();
                }

                return deleted;
            }
        }
    }

    public Secret? TryRead(string path)
    {
        Secret? secret = null;

        using (var readerConnection = new SqliteConnection(_connectionString))
        {
            readerConnection.Open();

            using (var cmd = readerConnection.CreateCommand())
            {
                if (TryReadSecret(readerConnection, path) is { } tuple)
                {
                    var (secretId, expiryDate) = tuple;

                    if (expiryDate.HasValue
                        && expiryDate.Value < DateTime.UtcNow)
                    {
                        Remove(path);
                    }
                    else
                    {
                        var pairs = GetPairs(readerConnection, secretId);

                        secret = new Secret(pairs, expiryDate);
                    }
                }
            }
        }

        return secret;
    }

    static (int, DateTime?)? TryReadSecret(SqliteConnection connection, string path)
    {
        (int, DateTime?)? tuple = null;

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT Id, ExpiryDate FROM Secret WHERE Path=@path";

            cmd.Parameters.AddWithValue("@path", path);

            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    var id = reader.GetInt32(0);

                    DateTime? expiryDate = null;

                    if (!reader.IsDBNull(1))
                    {
                        var ticks = reader.GetInt64(1);

                        expiryDate = new DateTime(ticks);
                    }

                    tuple = (id, expiryDate);
                }
            }
        }

        return tuple;
    }

    Dictionary<string, string> GetPairs(SqliteConnection connection, int secretId)
    {
        var pairs = new Dictionary<string, string>();

        using (var aes = CreateAes())
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Key, IV, Value FROM Pair WHERE SecretId=@secretId";

                cmd.Parameters.AddWithValue("@secretId", secretId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var key = reader.GetString(0);
                        var iv = reader.GetString(1).FromHex();
                        var encryptedValue = reader.GetString(2).FromHex();

                        aes.IV = iv;

                        using (var decryptor = aes.CreateDecryptor())
                        {
                            var ms = encryptedValue.ToMemoryStream();

                            using (var stream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                            {
                                using (var streamReader = new StreamReader(stream))
                                {
                                    var value = streamReader.ReadToEnd();

                                    pairs.Add(key, value);
                                }
                            }
                        }
                    }
                }
            }
        }

        return pairs;
    }

    public void CollectGarbage()
    {
        lock (_writerLock)
        {
            CollectGarbage_Unlocked();
        }
    }

    void CollectGarbage_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM Secret WHERE ExpiryDate<@ticks";

            cmd.Parameters.AddWithValue("@ticks", DateTime.UtcNow.Ticks);

            cmd.ExecuteNonQuery();
        }
    }

    void Vacuum_Unlocked()
    {
        using (var cmd = _writerConnection.CreateCommand())
        {
            cmd.CommandText = "VACUUM";
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _writerConnection.Dispose();
        _writerAes.Dispose();
    }
}