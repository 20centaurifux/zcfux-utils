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

public sealed class Writer : IWriter
{
    readonly Client _client;

    public Writer(Options options, HttpClient? client = null)
        => _client = new Client(options, client);

    public void Write(string path, Secret secret)
    {
        var deleteVersionAfter = "0s";

        var now = DateTime.UtcNow;

        if (secret.ExpiryDate.HasValue
            && secret.ExpiryDate.Value > now)
        {
            var diff = secret.ExpiryDate.Value - now;

            deleteVersionAfter = $"{Math.Round(diff.TotalSeconds)}s";
        }

        _client.PostMetadata(
            path,
            new Dictionary<string, object>
            {
                { "max_versions", 1 },
                { "cas_required", false },
                { "delete_version_after", deleteVersionAfter },
            });

        _client.PostSecret(
            path,
            secret.Data.ToDictionary(
                kv => kv.Key,
                kv => (object)kv.Value));
    }

    public void Remove(string path)
    {
        if (!_client.DeleteSecret(path))
        {
            throw new SecretNotFoundException($"Secret (path=`{path}') not found.");
        }
    }
}