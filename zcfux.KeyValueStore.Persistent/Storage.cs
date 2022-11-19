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
using zcfux.Byte;

namespace zcfux.KeyValueStore.Persistent;

internal sealed class Storage : IDisposable
{
    readonly string _path;
    DirectoryInfo? _blobs;
    DirectoryInfo? _tmp;
    Db? _db;

    public Storage(string path)
        => _path = path;

    public void Setup()
    {
        var directory = new DirectoryInfo(_path);

        directory.Create();

        _blobs = directory.CreateSubdirectory("blobs");
        _tmp = directory.CreateSubdirectory("tmp");

        RemoveTemporaryFiles();
        CreateDb();
    }

    void RemoveTemporaryFiles()
    {
        if (_tmp != null)
        {
            foreach (var directory in _tmp.GetDirectories())
            {
                directory.Delete(recursive: true);
            }

            foreach (var file in _tmp.GetFiles())
            {
                file.Delete();
            }
        }
    }

    void CreateDb()
    {
        _db = new Db(_path);

        _db.Setup();
    }

    public IEnumerable<string> GetKeys()
        => _db!.GetKeys();

    public void CollectGarbage()
    {
        RemoveOrphanFiles();
        RemoveOrphanBlobsFromDb();
        Vacuum();
    }

    void RemoveOrphanFiles()
    {
        var orphans = new BlobDirectory(_blobs!.FullName).FindAll()
            .Where(t => _db!.IsOrphan(t.Item1))
            .Select(t => new FileInfo(t.Item2))
            .ToArray();

        foreach (var orphan in orphans)
        {
            if (FileLock.TryEnterWriteLock(orphan.FullName, TimeSpan.FromSeconds(5)))
            {
                try
                {
                    orphan.Delete();
                }
                finally
                {
                    FileLock.ExitWriteLock(orphan.FullName);
                }
            }
        }
    }

    void RemoveOrphanBlobsFromDb()
        => _db!.RemoveOrphans();

    void Vacuum()
        => _db!.Vacuum();

    public FileStream CreateTemporaryFile()
    {
        var path = Path.Combine(_tmp!.FullName, RandomFilename());

        return File.Create(path);
    }

    static string RandomFilename()
        => Guid
            .NewGuid()
            .ToString()
            .Replace("-", string.Empty);

    public void StoreSmallBlob(string key, byte[] hash, byte[] contents)
    {
        var hashAsHex = hash.ToHex();

        _db!.Put(key, hashAsHex, contents);
    }

    public void MoveBlob(string key, byte[] hash, string filename)
    {
        var hashAsHex = hash.ToHex();

        var (directory, name) = BuildBlobPath(hashAsHex);

        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, name);

        try
        {
            FileLock.EnterWriteLock(fullPath);

            File.Move(filename, fullPath, overwrite: true);
        }
        finally
        {
            FileLock.ExitWriteLock(fullPath);
        }

        _db!.Associate(key, hashAsHex);
    }

    public Stream Fetch(string key)
    {
        var (_, hashAsHex, contents) = _db!.Fetch(key);

        return (contents == null)
            ? CreateStreamFromFile(hashAsHex)
            : CreateStreamFromContents(contents);
    }

    Stream CreateStreamFromFile(string hashAsHex)
    {
        var (directory, filename) = BuildBlobPath(hashAsHex);

        var fullname = Path.Combine(directory, filename);

        var fileStream = File.OpenRead(fullname);

        return new BlobFileReader(fileStream);
    }

    (string, string) BuildBlobPath(string hash)
    {
        var l = new List<string>()
        {
            _blobs!.FullName
        };

        for (var offset = 0; offset < 32; offset += 4)
        {
            l.Add(hash.Substring(offset, 4));
        }

        var directory = Path.Combine(l.ToArray());

        return (directory, hash[32..]);
    }

    static Stream CreateStreamFromContents(byte[] contents)
        => new MemoryStream(contents);

    public void Remove(string key)
        => _db!.Remove(key);

    public void Dispose()
        => _db?.Dispose();
}