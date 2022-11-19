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
using System.Net;
using System.Text;
using Newtonsoft.Json;
using zcfux.CredentialStore.Vault.Model;

namespace zcfux.CredentialStore.Vault;

internal sealed class Client
{
    readonly Options _options;
    readonly HttpClient _client;

    public Client(Options options, HttpClient? client = null)
        => (_options, _client) = (options, client ?? new HttpClient());

    public SecretVersionResponse? GetSecret(string path)
    {
        SecretVersionResponse? secretVersion = null;

        var request = BuildGetRequest($"/v1/{_options.MountPoint}/data/{path.Trim('/')}");

        var response = _client.Send(request);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            secretVersion = ReadResponse<SecretVersionResponse>(response);

            if (secretVersion is not { })
            {
                throw new Exception("Couldn't parse Vault response.");
            }
        }
        else if (response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new UnexpectedStatusCodeException(response.StatusCode);
        }

        return secretVersion;
    }

    HttpRequestMessage BuildGetRequest(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, _options.Url + path);

        request.Headers.Add("X-Vault-Token", _options.Token);

        return request;
    }

    static T? ReadResponse<T>(HttpResponseMessage response)
        where T : class
    {
        T? result = null;

        if (response.IsSuccessStatusCode)
        {
            using (var stream = response.Content.ReadAsStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();

                    result = JsonConvert.DeserializeObject<T>(json);
                }
            }
        }

        return result;
    }

    public void PostMetadata(string path, IDictionary<string, object> metaData)
    {
        var request = BuildPostRequest(
            $"/v1/{_options.MountPoint}/metadata/{path.Trim('/')}",
            metaData);

        var response = _client.Send(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new UnexpectedStatusCodeException(response.StatusCode);
        }
    }

    public void PostSecret(string path, IDictionary<string, object> data)
    {
        var body = new
        {
            data = new Dictionary<string, object>(data)
        };

        var request = BuildPostRequest(
            $"/v1/{_options.MountPoint}/data/{path.Trim('/')}",
            body);

        var response = _client.Send(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new UnexpectedStatusCodeException(response.StatusCode);
        }
    }

    HttpRequestMessage BuildPostRequest(string path, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.Url + path);

        request.Headers.Add("X-Vault-Token", _options.Token);

        var json = JsonConvert.SerializeObject(body);

        request.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return request;
    }

    public bool DeleteSecret(string path)
    {
        var request = BuildDeleteRequest($"/v1/{_options.MountPoint}/metadata/{path.Trim('/')}");

        var response = _client.Send(request);

        var deleted = response.IsSuccessStatusCode;

        if (!response.IsSuccessStatusCode
            && response.StatusCode != HttpStatusCode.NotFound)
        {
            throw new UnexpectedStatusCodeException(response.StatusCode);
        }

        return deleted;
    }

    HttpRequestMessage BuildDeleteRequest(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, _options.Url + path);

        request.Headers.Add("X-Vault-Token", _options.Token);

        return request;
    }
}