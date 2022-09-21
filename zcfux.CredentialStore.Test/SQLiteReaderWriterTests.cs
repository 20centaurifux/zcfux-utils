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
using NUnit.Framework;

namespace zcfux.CredentialStore.Test;

public sealed class SQLiteReaderWriterTests : AReaderWriterTests
{
    string? _filename = null;
    string? _password = null;

    public override void Teardown()
    {
        base.Teardown();

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (_filename != null)
        {
            File.Delete(_filename);
        }
    }

    protected override IStore CreateAndSetupStore()
    {
        _filename = Path.GetTempFileName();
        _password = TestContext.CurrentContext.Random.GetString();

        var store = new SQLite.Store(_filename, _password);

        store.Setup();

        return store;
    }

    [Test]
    public void InvalidPassword()
    {
        var password = TestContext.CurrentContext.Random.GetString();

        using (var store = new SQLite.Store(_filename!, password))
        {
            Assert.Throws<SQLite.InvalidPasswordException>(() => store.Setup());
        }
    }
}