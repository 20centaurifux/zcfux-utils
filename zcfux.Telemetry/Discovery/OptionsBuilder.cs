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
using zcfux.Logging;

namespace zcfux.Telemetry.Discovery;

public sealed class OptionsBuilder
{
    IConnection? _connection;
    ImmutableList<NodeFilter> _filters = ImmutableList<NodeFilter>.Empty;
    ApiRegistry? _apiRegistry;
    ISerializer? _serializer;
    ILogger? _logger;

    OptionsBuilder Clone()
    {
        var builder = new OptionsBuilder
        {
            _connection = _connection,
            _filters = _filters,
            _apiRegistry = _apiRegistry,
            _serializer = _serializer,
            _logger = _logger
        };

        return builder;
    }

    public OptionsBuilder WithConnection(IConnection connection)
    {
        var builder = Clone();

        builder._connection = connection;

        return builder;
    }

    public OptionsBuilder WithFilter(NodeFilter nodeFilter)
    {
        var builder = Clone();

        builder._filters = builder._filters.Add(nodeFilter);

        return builder;
    }

    public OptionsBuilder WithApiRegistry(ApiRegistry apiRegistry)
    {
        var builder = Clone();

        builder._apiRegistry = apiRegistry;

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
            _connection!,
            _filters,
            _apiRegistry!,
            _serializer!,
            _logger);
    }

    void ThrowIfIncomplete()
    {
        if (_connection == null)
        {
            throw new ArgumentException("Connection cannot be null.");
        }

        if (_apiRegistry == null)
        {
            throw new ArgumentException("ApiRegistry cannot be null.");
        }

        if (_serializer == null)
        {
            throw new ArgumentException("Serializer cannot be null.");
        }
    }
}