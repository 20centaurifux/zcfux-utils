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
using LinqToDB.Configuration;
using NUnit.Framework;

namespace zcfux.Data.Test;

public sealed class PureProxyTests : APureTests
{
    const string DefaultConnectionString
        = "User ID=test;Host=localhost;Port=5432;Database=test;";

    [Test]
    public void CreateProxies()
    {
        var first = Proxy.Factory.ConvertHandle<IPure, LinqToDB.Pure>();

        Assert.IsInstanceOf<IPure>(first);

        var second = Proxy.Factory.ConvertHandle<IPure, LinqToDB.Pure>(new LinqToDB.Pure());

        Assert.IsInstanceOf<IPure>(second);
    }

    protected override IEngine NewEngine()
    {
        var connectionString = Environment.GetEnvironmentVariable("PG_TEST_CONNECTIONSTRING")
                               ?? DefaultConnectionString;

        var builder = new LinqToDbConnectionOptionsBuilder();

        builder.UsePostgreSQL(connectionString);

        var opts = builder.Build();

        return new LinqToDB.Engine(opts);
    }

    protected override IPure NewDb()
        => Proxy.Factory.ConvertHandle<IPure, LinqToDB.Pure>();
}