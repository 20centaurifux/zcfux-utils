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
namespace zcfux.Pool;

public sealed class PoolBuilder<T>
    where T : AResource
{
    int _limit = 10;
    IFactory<T>? _factory = null;
    IKeyBuilder _keyBuilder = new AbsoluteUri();

    public PoolBuilder<T> WithLimit(int limit)
    {
        return new PoolBuilder<T>
        {
            _limit = limit,
            _factory = _factory,
            _keyBuilder = _keyBuilder
        };
    }

    public PoolBuilder<T> WithFactory(IFactory<T> factory)
    {
        return new PoolBuilder<T>
        {
            _limit = _limit,
            _factory = factory,
            _keyBuilder = _keyBuilder
        };
    }

    public PoolBuilder<T> WithFactory(Func<Uri, T> fn)
    {
        return new PoolBuilder<T>
        {
            _limit = _limit,
            _factory = new FactoryWrapper<T>(fn),
            _keyBuilder = _keyBuilder
        };
    }

    public PoolBuilder<T> WithKeyBuilder(IKeyBuilder keyBuilder)
    {
        return new PoolBuilder<T>
        {
            _limit = _limit,
            _factory = _factory,
            _keyBuilder = keyBuilder
        };
    }

    public PoolBuilder<T> WithKeyBuilder(Func<Uri, string> fn)
    {
        return new PoolBuilder<T>
        {
            _limit = _limit,
            _factory = _factory,
            _keyBuilder = new KeyBuilderWrapper(fn)
        };
    }

    public Pool<T> Build()
    {
        var options = new Options
        {
            Limit = _limit
        };

        return new Pool<T>(_factory!, _keyBuilder, options);
    }
}