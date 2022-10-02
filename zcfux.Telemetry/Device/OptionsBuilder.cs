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
using zcfux.Logging;

namespace zcfux.Telemetry.Device
{
    public sealed class OptionsBuilder
    {
        string? _domain;
        string? _kind;
        int? _id;
        IConnection? _connection;
        ISerializer? _serializer;
        ILogger? _logger;

        OptionsBuilder Clone()
        {
            var builder = new OptionsBuilder
            {
                _domain = _domain,
                _kind = _kind,
                _id = _id,
                _connection = _connection,
                _serializer = _serializer,
                _logger = _logger
            };

            return builder;
        }

        public OptionsBuilder WithDomain(string domain)
        {
            var builder = Clone();

            builder._domain = domain;

            return builder;
        }

        public OptionsBuilder WithKind(string kind)
        {
            var builder = Clone();

            builder._kind = kind;

            return builder;
        }

        public OptionsBuilder WithId(int id)
        {
            var builder = Clone();

            builder._id = id;

            return builder;
        }

        public OptionsBuilder WithDevice(DeviceDetails device)
        {
            var builder = Clone();

            builder._domain = device.Domain;
            builder._kind = device.Kind;
            builder._id = device.Id;

            return builder;
        }

        public OptionsBuilder WithConnection(IConnection connection)
        {
            var builder = Clone();

            builder._connection = connection;

            return builder;
        }

        public OptionsBuilder WithSerializer(ISerializer serializer)
        {
            var builder = Clone();

            builder._serializer = serializer;

            return builder;
        }

        public OptionsBuilder WithLogger(ILogger? logger)
        {
            var builder = Clone();

            builder._logger = logger;

            return builder;
        }

        public Options Build()
        {
            ThrowIfIncomplete();

            return new Options(
                new DeviceDetails(_domain!, _kind!, _id!.Value),
                _connection!,
                _serializer!,
                _logger);
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

            if (_connection == null)
            {
                throw new ArgumentException("Connection cannot be null.");
            }

            if (_serializer == null)
            {
                throw new ArgumentException("Serializer cannot be null.");
            }
        }
    }
}