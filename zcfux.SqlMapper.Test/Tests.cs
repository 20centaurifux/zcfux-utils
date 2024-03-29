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
using System.Data;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using zcfux.SqlMapper.Test.Model;

namespace zcfux.SqlMapper.Test;

public sealed class Tests
{
    IDbConnection _conn = default!;

    [SetUp]
    public void Setup()
    {
        _conn = CreateConnection();

        SetupDb();
    }

    [TearDown]
    public void Teardown()
        => _conn.Close();

    static IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection("Data Source=:memory:");

        conn.Open();

        return conn;
    }

    void SetupDb()
    {
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE T (ID INTEGER, A VARCHAR(64), B VARCHAR(64))";

            cmd.ExecuteNonQuery();
        }
    }

    [Test]
    public void ToObject()
    {
        var id = TestContext.CurrentContext.Random.Next();
        var a = TestContext.CurrentContext.Random.GetString();
        var b = TestContext.CurrentContext.Random.GetString();

        InsertRecord((id, a, b));

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            using (var r = cmd.ExecuteReader())
            {
                r.Read();

                var obj = r.ToObject<Foo>();

                Assert.AreEqual(id, obj.ID);
                Assert.AreEqual(a, obj.A);
                Assert.AreEqual(b, obj.B);
            }
        }
    }

    [Test]
    public void ToModel()
    {
        var id = TestContext.CurrentContext.Random.Next();
        var a = TestContext.CurrentContext.Random.GetString();
        var b = TestContext.CurrentContext.Random.GetString();

        InsertRecord((id, a, b));

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            using (var r = cmd.ExecuteReader())
            {
                r.Read();

                var obj = r.ToObject<Bar>();

                Assert.AreEqual(id, obj.Identifier);
                Assert.AreEqual(a, obj.First);
                Assert.AreEqual(b, obj.Second);
            }
        }
    }

    [Test]
    public void ReadAndMap()
    {
        var id = TestContext.CurrentContext.Random.Next();
        var a = TestContext.CurrentContext.Random.GetString();
        var b = TestContext.CurrentContext.Random.GetString();

        InsertRecord((id, a, b));

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            using (var r = cmd.ExecuteReader())
            {
                var obj = r.ReadAndMap<Foo>();

                Assert.AreEqual(id, obj!.ID);
                Assert.AreEqual(a, obj.A);
                Assert.AreEqual(b, obj.B);

                obj = r.ReadAndMap<Foo>();

                Assert.IsNull(obj);
            }
        }
    }

    [Test]
    public void FetchOne()
    {
        var id = TestContext.CurrentContext.Random.Next();
        var a = TestContext.CurrentContext.Random.GetString();
        var b = TestContext.CurrentContext.Random.GetString();

        InsertRecord((id, a, b));

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            var obj = cmd.FetchOne<Foo>();

            Assert.AreEqual(id, obj!.ID);
            Assert.AreEqual(a, obj.A);
            Assert.AreEqual(b, obj.B);
        }
    }

    [Test]
    public void FetchLazy()
    {
        var a = (TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString());

        var b = (TestContext.CurrentContext.Random.Next(),
            TestContext.CurrentContext.Random.GetString(),
            TestContext.CurrentContext.Random.GetString());

        InsertRecord(a);
        InsertRecord(b);

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            var entries = cmd.FetchLazy<Foo>().ToArray();

            Assert.AreEqual(2, entries.Length);

            var fetchedA = entries.Single(obj => obj.ID == a.Item1);

            Assert.AreEqual(a.Item1, fetchedA.ID);
            Assert.AreEqual(a.Item2, fetchedA.A);
            Assert.AreEqual(a.Item3, fetchedA.B);

            var fetchedB = entries.Single(obj => obj.ID == b.Item1);

            Assert.AreEqual(b.Item1, fetchedB.ID);
            Assert.AreEqual(b.Item2, fetchedB.A);
            Assert.AreEqual(b.Item3, fetchedB.B);
        }
    }

    void InsertRecord((int, string, string) record)
    {
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO T (ID, A, B) VALUES (@ID, @A, @B)";

            var param = cmd.CreateParameter();

            param.ParameterName = "ID";
            param.Value = record.Item1;

            cmd.Parameters.Add(param);

            param = cmd.CreateParameter();

            param.ParameterName = "A";
            param.Value = record.Item2;

            cmd.Parameters.Add(param);

            param = cmd.CreateParameter();

            param.ParameterName = "B";
            param.Value = record.Item3;

            cmd.Parameters.Add(param);

            cmd.ExecuteNonQuery();
        }
    }

    [Test]
    public void AssignObject()
    {
        var obj = new Foo()
        {
            ID = TestContext.CurrentContext.Random.Next(),
            A = TestContext.CurrentContext.Random.GetString(),
            B = TestContext.CurrentContext.Random.GetString(),
        };

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO T (ID, A, B) VALUES (@ID, @A, @B)";

            cmd.Assign(obj);

            cmd.ExecuteNonQuery();
        }

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            var fetched = cmd.FetchOne<Foo>();

            Assert.AreEqual(obj.ID, fetched!.ID);
            Assert.AreEqual(obj.A, fetched.A);
            Assert.AreEqual(obj.B, fetched.B);
        }
    }

    [Test]
    public void AssignModel()
    {
        var obj = new Bar()
        {
            Identifier = TestContext.CurrentContext.Random.Next(),
            First = TestContext.CurrentContext.Random.GetString(),
            Second = TestContext.CurrentContext.Random.GetString(),
        };

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO T (ID, A, B) VALUES (@ID, @A, @B)";

            cmd.Assign(obj);

            cmd.ExecuteNonQuery();
        }

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            var fetched = cmd.FetchOne<Bar>();

            Assert.AreEqual(obj.Identifier, fetched!.Identifier);
            Assert.AreEqual(obj.First, fetched.First);
            Assert.AreEqual(obj.Second, fetched.Second);
        }
    }

    [Test]
    public void AssignObjectWithCustomKeyFn()
    {
        var obj = new Foo()
        {
            ID = TestContext.CurrentContext.Random.Next(),
            A = TestContext.CurrentContext.Random.GetString(),
            B = TestContext.CurrentContext.Random.GetString(),
        };

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO T (ID, A, B) VALUES (<ID>, <A>, <B>)";

            cmd.Assign(obj, prop => $"<{prop}>");

            cmd.ExecuteNonQuery();
        }

        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM T";

            var fetched = cmd.FetchOne<Foo>();

            Assert.AreEqual(obj.ID, fetched!.ID);
            Assert.AreEqual(obj.A, fetched.A);
            Assert.AreEqual(obj.B, fetched.B);
        }
    }
}