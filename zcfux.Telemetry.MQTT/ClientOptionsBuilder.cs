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
namespace zcfux.Telemetry.MQTT;

public sealed class ClientOptionsBuilder
{
    public const int DefaultPort = 1883;
    public const int DefaultTlsPort = 8883;

    string _address = "localhost";
    int? _port;
    bool _tls;
    bool _allowUntrustedCertificates;
    TimeSpan _timeout = TimeSpan.FromSeconds(30);
    LastWillOptions? _lastWillOptions;

    ClientOptionsBuilder Clone()
    {
        var builder = new ClientOptionsBuilder
        {
            _address = _address,
            _port = _port,
            _tls = _tls,
            _allowUntrustedCertificates = _allowUntrustedCertificates,
            _timeout = _timeout,
            _lastWillOptions = _lastWillOptions
        };

        return builder;
    }

    public ClientOptionsBuilder WithAddress(string address)
    {
        var builder = Clone();

        builder._address = address;

        return builder;
    }

    public ClientOptionsBuilder WithPort(int port)
    {
        var builder = Clone();

        builder._port = port;

        return builder;
    }

    public ClientOptionsBuilder WithTls()
    {
        var builder = Clone();

        builder._tls = true;

        return builder;
    }

    public ClientOptionsBuilder WithoutTls()
    {
        var builder = Clone();

        builder._tls = false;

        return builder;
    }

    public ClientOptionsBuilder WithAllowUntrustedCertificates()
    {
        var builder = Clone();

        builder._allowUntrustedCertificates = false;

        return builder;
    }

    public ClientOptionsBuilder WithTimeout(TimeSpan timeout)
    {
        var builder = Clone();

        builder._timeout = timeout;

        return builder;
    }

    public ClientOptionsBuilder WithLastWill(LastWillOptions lastWill)
    {
        var builder = Clone();

        builder._lastWillOptions = lastWill;

        return builder;
    }

    public ClientOptions Build()
    {
        var port = _port.GetValueOrDefault(_tls
            ? DefaultTlsPort
            : DefaultPort);

        return new ClientOptions(
            _address,
            port,
            _tls,
            _allowUntrustedCertificates,
            _timeout,
            _lastWillOptions);
    }
}