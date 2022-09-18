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
namespace zcfux.CredentialStore.Vault;

public sealed class Reader : IReader
{
    readonly Client _client;

    public Reader(Options options, HttpClient? client = null)
        => _client = new Client(options, client);

    public Secret Read(string path)
    {
        Secret? secret = null;

        var version = _client.GetSecret(path);

        if (version is { Data.Meta.Destroyed: false }
            && (!version.Data.Meta.ExpiryDate.HasValue
                || version.Data.Meta.ExpiryDate > DateTime.UtcNow))
        {
            secret = new Secret(
                version.Data.Data.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.ToString() ?? string.Empty),
                version.Data.Meta.ExpiryDate);
        }

        return secret
               ?? throw new SecretNotFoundException($"Secret (path=`{path}') not found.");
    }
}