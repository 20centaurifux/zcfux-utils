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

public sealed class VaultReaderWriterTests : AReaderWriterTests
{
    VaultServer? _server;
    
    [SetUp]
    public override void Setup()
    {
        base.Setup();
        
        _server = new VaultServer();

        _server.Start();
    }

    [TearDown]
    public void Teardown()
        => _server?.Stop();
    
    protected override IStore CreateAndSetupStore()
    {
        var options = BuildOptions();

        var client = new HttpClient();

        var store = new Builder()
            .WithReader(new Vault.Reader(options, client))
            .WithWriter(new Vault.Writer(options, client))
            .Build();

        store.Setup();

        return store;
    }
    
    static Vault.Options BuildOptions()
    {
        var url = Environment.GetEnvironmentVariable("VAULT_TEST_URL")
                  ?? "http://127.0.0.1:8200";

        var options = new Vault.Options(url, "root");

        return options;
    }
}