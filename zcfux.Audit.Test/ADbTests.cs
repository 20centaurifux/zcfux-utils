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
using zcfux.Data;

namespace zcfux.Audit.Test;

public abstract class ADbTest
{
    IEngine? _engine;
    Transaction? _transaction;
    protected object? _handle;
    protected IAuditDb? _db;

    [SetUp]
    public virtual void Setup()
    {
        _engine = Methods.NewEngine();

        _transaction = _engine.NewTransaction();

        _handle = _transaction.Handle;

        Methods.DeleteAll(_handle);

        _db = Methods.CreateDb();
    }

    [TearDown]
    public void Teardown()
    {
        _transaction.Commit = true;
        _transaction?.Dispose();
    }
    //=> _transaction?.Dispose();

    protected abstract IDbTestMethods Methods { get; }
}