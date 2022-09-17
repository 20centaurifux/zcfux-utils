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
namespace zcfux.CredentialStore;

public sealed class Builder
{
    sealed class Store : IStore
    {
        readonly IReader? _reader;
        readonly IWriter? _writer;

        public Store(IReader? reader, IWriter? writer)
            => (_reader, _writer) = (reader, writer);

        public void Setup()
        {
        }

        public bool CanRead
            => (_reader != null);

        public IReader CreateReader()
            => _reader
                ?? throw new NotImplementedException();

        public bool CanWrite
            => (_writer != null);

        public IWriter CreateWriter()
            => _writer
                ?? throw new NotImplementedException();
    }

    IReader? _reader;
    IWriter? _writer;

    Builder Clone()
        => new()
        {
            _reader = _reader,
            _writer = _writer
        };

    public Builder WithReader(IReader reader)
    {
        var builder = Clone();

        builder._reader = reader;

        return builder;
    }

    public Builder WithCachingReader(IReader reader, CachingReader.Options options)
        => WithReader(
            new CachingReader(reader, options));

    public Builder WithWriter(IWriter writer)
    {
        var builder = Clone();

        builder._writer = writer;

        return builder;
    }

    public IStore Build()
        => new Store(_reader, _writer);
}