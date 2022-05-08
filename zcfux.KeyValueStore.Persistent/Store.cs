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

public sealed class Store : IStore
{
    public sealed record Options(string StoragePath, int SwapThreshold);

    readonly Options _options;
    Storage? _storage;

    public Store(Options options)
        => _options = options;

    public void Setup()
    {
        _storage = new Storage(_options.StoragePath);

        _storage.Setup();
    }

    public void CollectGarbage()
        => _storage?.CollectGarbage();

    public void Put(string key, Stream stream)
    {
        if (stream.CanSeek && stream.Length < _options.SwapThreshold)
        {
            StoreSmallBlob(key, stream);
        }
        else
        {
            ReceiveAndStoreBlob(key, stream);
        }
    }

    void StoreSmallBlob(string key, Stream stream)
    {
        var copyUtil = new CopyUtil();

        var ms = new MemoryStream();

        copyUtil.Copy(stream, ms);

        _storage!.StoreSmallBlob(key, copyUtil.Hash!, ms.ToArray());
    }

    void ReceiveAndStoreBlob(string key, Stream stream)
    {
        using (var tmpStream = _storage!.CreateTemporaryFile())
        {
            var copyUtil = new CopyUtil();

            copyUtil.Copy(stream, tmpStream);

            if (copyUtil.Length < _options.SwapThreshold)
            {
                var ms = new MemoryStream();

                tmpStream.Seek(0, SeekOrigin.Begin);
                tmpStream.CopyTo(ms);

                _storage!.StoreSmallBlob(key, copyUtil.Hash!, ms.ToArray());

                tmpStream.Close();

                File.Delete(tmpStream.Name);
            }
            else
            {
                tmpStream.Close();

                _storage.MoveBlob(key, copyUtil.Hash!, tmpStream.Name);
            }
        }
    }

    public Stream Fetch(string key)
        => _storage!.Fetch(key);

    public void Remove(string key)
        => _storage!.Remove(key);

    public void Dispose()
        => _storage?.Dispose();
}