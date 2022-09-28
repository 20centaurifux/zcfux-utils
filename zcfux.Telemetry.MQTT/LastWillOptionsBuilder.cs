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

public sealed class LastWillOptionsBuilder
{
    string? _domain;
    string? _kind;
    int? _id;
    MessageOptions? _messageOptions;

    LastWillOptionsBuilder Clone()
    {
        var builder = new LastWillOptionsBuilder
        {
            _domain = _domain,
            _kind = _kind,
            _id = _id,
            _messageOptions = _messageOptions
        };

        return builder;
    }

    public LastWillOptionsBuilder WithDomain(string domain)
    {
        var builder = Clone();

        builder._domain = domain;

        return builder;
    }

    public LastWillOptionsBuilder WithKind(string kind)
    {
        var builder = Clone();

        builder._kind = kind;

        return builder;
    }

    public LastWillOptionsBuilder WithId(int id)
    {
        var builder = Clone();

        builder._id = id;

        return builder;
    }

    public LastWillOptionsBuilder WithDevice(DeviceDetails device)
    {
        var builder = Clone();

        builder._domain = device.Domain;
        builder._kind = device.Kind;
        builder._id = device.Id;

        return builder;
    }

    public LastWillOptionsBuilder WithMessageOptions(MessageOptions messageOptions)
    {
        var builder = Clone();

        builder._messageOptions = messageOptions;

        return builder;
    }

    public LastWillOptions Build()
    {
        ThrowIfIncomplete();

        return new LastWillOptions(
            new DeviceDetails(_domain!, _kind!, _id!.Value),
            _messageOptions!);
    }

    void ThrowIfIncomplete()
    {
        if (_domain == null)
        {
            throw new ArgumentException("Domain cannot be null.");
        }

        if (_kind == null)
        {
            throw new ArgumentException("Kind cannot be null.");
        }

        if (_id == null)
        {
            throw new ArgumentException("Id cannot be null.");
        }

        if (_messageOptions == null)
        {
            throw new ArgumentException("MessageOptions cannot be null.");
        }
    }
}