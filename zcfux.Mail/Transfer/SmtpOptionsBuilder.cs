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
using System.Collections.Immutable;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace zcfux.Mail.Transfer;

public sealed class SmtpOptionsBuilder
{
    string _host = "localhost";
    int _port = 497;
    ESecureSocketOptions _secureSocketOptions = ESecureSocketOptions.ImplicitTls;
    string? _username;
    string? _password;
    ImmutableList<X509Certificate> _clientCertificates = ImmutableList.Create<X509Certificate>();
    RemoteCertificateValidationCallback? _serverCertificateValidationCallback;
    bool _keepOpen;

    sealed record SmtpOptions(
        string Host,
        int Port,
        ESecureSocketOptions SecureSocketOptions,
        string? Username,
        string? Password,
        X509Certificate[] ClientCertificates,
        RemoteCertificateValidationCallback? ServerCertificateValidationCallback,
        bool KeepOpen
    ) : ISmtpOptions;

    SmtpOptionsBuilder Clone()
        => new()
        {
            _host = _host,
            _port = _port,
            _secureSocketOptions = _secureSocketOptions,
            _username = _username,
            _password = _password,
            _clientCertificates = _clientCertificates,
            _serverCertificateValidationCallback = _serverCertificateValidationCallback,
            _keepOpen = _keepOpen
        };

    public SmtpOptionsBuilder WithHost(string host)
    {
        var builder = Clone();

        builder._host = host;

        return builder;
    }

    public SmtpOptionsBuilder WithPort(int port)
    {
        var builder = Clone();

        builder._port = port;

        return builder;
    }

    public SmtpOptionsBuilder WithSecureSocketOptions(ESecureSocketOptions secureSocketOptions)
    {
        var builder = Clone();

        builder._secureSocketOptions = secureSocketOptions;

        return builder;
    }

    public SmtpOptionsBuilder WithUsername(string username)
    {
        var builder = Clone();

        builder._username = username;

        return builder;
    }

    public SmtpOptionsBuilder WithPassword(string password)
    {
        var builder = Clone();

        builder._password = password;

        return builder;
    }

    public SmtpOptionsBuilder WithClientCertificate(X509Certificate clientCertificate)
    {
        var builder = Clone();

        builder._clientCertificates = builder._clientCertificates.Add(clientCertificate);

        return builder;
    }

    public SmtpOptionsBuilder WithServerCertificateValidationCallback(
        RemoteCertificateValidationCallback serverCertificateValidationCallback)
    {
        var builder = Clone();

        builder._serverCertificateValidationCallback = serverCertificateValidationCallback;

        return builder;
    }

    public SmtpOptionsBuilder WithDisabledServerCertificateValidation()
        => WithServerCertificateValidationCallback((_, _, _, _) => true);

    public SmtpOptionsBuilder WithKeepOpen()
    {
        var builder = Clone();

        builder._keepOpen = true;

        return builder;
    }

    public SmtpOptionsBuilder WithoutKeepOpen()
    {
        var builder = Clone();

        builder._keepOpen = false;

        return builder;
    }

    public ISmtpOptions Build()
        => new SmtpOptions(
            _host,
            _port,
            _secureSocketOptions,
            _username,
            _password,
            _clientCertificates.ToArray(),
            _serverCertificateValidationCallback,
            _keepOpen);
}