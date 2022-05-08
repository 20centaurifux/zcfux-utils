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
using System.Security.Cryptography;
using zcfux.Byte;

namespace zcfux.KeyValueStore.Persistent;

public sealed class Fsck
{
    public event EventHandler<FsckEventArgs>? Corrupted;
    public event EventHandler<FsckEventArgs>? Deleted;
    public event EventHandler<FsckEventArgs>? Missing;

    readonly string _path;

    public Fsck(string path)
        => _path = path;

    public bool Dry { get; set; }

    public void CheckDatabase()
    {
        using (var db = new Db(_path))
        {
            db.Setup();

            var corrupted = CollectCorruptedSmallBlobs(db).ToArray();

            foreach (var hash in corrupted)
            {
                Corrupted?.Invoke(this, new FsckEventArgs(hash, ELocation.Database));

                if (!Dry)
                {
                    if (db.RemoveBlobAssociations(hash) > 0)
                    {
                        Deleted?.Invoke(this, new FsckEventArgs(hash, ELocation.Index));
                    }

                    if (db.RemoveSmallBlob(hash))
                    {
                        Deleted?.Invoke(this, new FsckEventArgs(hash, ELocation.Database));
                    }
                }
            }
        }
    }

    static IEnumerable<string> CollectCorruptedSmallBlobs(Db db)
    {
        foreach (var (hash, blob) in db.FetchSmallBlobs())
        {
            if (!TestSmallBlob(hash, blob))
            {
                yield return hash;
            }
        }
    }

    static bool TestSmallBlob(string hash, byte[] blob)
    {
        var computedHash = SHA256.HashData(blob).ToHex();

        return (hash == computedHash);
    }

    public void CheckFilesystem()
    {
        using (var db = new Db(_path))
        {
            db.Setup();

            var blobsPath = Path.Combine(_path, "blobs");

            var blobs = new BlobDirectory(blobsPath);

            foreach (var (hash, filename) in blobs.FindAll())
            {
                if (!TestLargeBlob(hash, filename))
                {
                    Corrupted?.Invoke(this, new FsckEventArgs(hash, ELocation.Filesystem));

                    if (!Dry)
                    {
                        File.Delete(filename);

                        if (db.RemoveBlobAssociations(hash) > 0)
                        {
                            Deleted?.Invoke(this, new FsckEventArgs(hash, ELocation.Index));
                        }

                        Deleted?.Invoke(this, new FsckEventArgs(hash, ELocation.Filesystem));
                    }
                }
            }
        }
    }

    static bool TestLargeBlob(string hash, string filename)
    {
        using (var stream = File.OpenRead(filename))
        {
            var sha256 = SHA256.Create();

            var computedHash = sha256.ComputeHash(stream).ToHex();

            return (hash == computedHash);
        }
    }

    public void CheckIndex()
    {
        using (var db = new Db(_path))
        {
            db.Setup();

            var largeBlobs = db.FetchLargeBlobs().ToArray();

            foreach (var hash in largeBlobs)
            {
                var fullPath = PathBuilder.BuildBlobPath(_path, hash);

                if (!File.Exists(fullPath))
                {
                    Missing?.Invoke(this, new FsckEventArgs(hash, ELocation.Filesystem));

                    if (!Dry)
                    {
                        if (db.RemoveBlobAssociations(hash) > 0)
                        {
                            Deleted?.Invoke(this, new FsckEventArgs(hash, ELocation.Index));
                        }
                    }
                }
            }
        }
    }
}