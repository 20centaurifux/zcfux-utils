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
namespace zcfux.KeyValueStore.Persistent;

internal sealed class BlobFileReader : Stream
{
    bool _disposed;
    readonly FileStream _stream;

    public BlobFileReader(FileStream stream)
    {
        FileLock.EnterReadLock(stream.Name);

        _stream = stream;
    }

    public override void Flush()
        => _stream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
        => _stream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin)
        => _stream.Seek(offset, origin);

    public override void SetLength(long value)
        => _stream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
        => _stream.Write(buffer, offset, count);

    public override bool CanRead => _stream.CanRead;

    public override bool CanSeek => _stream.CanSeek;

    public override bool CanWrite => _stream.CanWrite;

    public override long Length => _stream.Length;

    public override long Position
    {
        get => _stream.Position;

        set => _stream.Position = value;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!_disposed && disposing)
        {
            _stream.Dispose();
            
            FileLock.ExitReadLock(_stream.Name);
        }

        _disposed = true;
    }
}