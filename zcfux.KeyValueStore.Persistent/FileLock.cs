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

static class FileLock
{
    static readonly object Lock = new();
    static readonly Dictionary<string, ReaderWriterLockSlim> Map = new();

    public static void EnterReadLock(string path)
    {
        lock (Lock)
        {
            var lockSlim = GetLockSlim_Unlocked(path);

            lockSlim.EnterReadLock();
        }
    }

    public static void ExitReadLock(string path)
    {
        lock (Lock)
        {
            var lockSlim = GetLockSlim_Unlocked(path);

            lockSlim.ExitReadLock();

            if (lockSlim.TryEnterWriteLock(TimeSpan.FromMilliseconds(0)))
            {
                Map.Remove(path);
            }
        }
    }

    public static void EnterWriteLock(string path)
    {
        lock (Lock)
        {
            var lockSlim = GetLockSlim_Unlocked(path);

            lockSlim.EnterWriteLock();
        }
    }

    public static bool TryEnterWriteLock(string path, TimeSpan timeout)
    {
        lock (Lock)
        {
            var lockSlim = GetLockSlim_Unlocked(path);

            return lockSlim.TryEnterWriteLock(timeout);
        }
    }

    public static void ExitWriteLock(string path)
    {
        lock (Lock)
        {
            var lockSlim = GetLockSlim_Unlocked(path);

            lockSlim.ExitWriteLock();

            Map.Remove(path);
        }
    }

    static ReaderWriterLockSlim GetLockSlim_Unlocked(string path)
    {
        var lockSlim = Map.GetValueOrDefault(path);

        if (lockSlim == null)
        {
            lockSlim = new ReaderWriterLockSlim();

            Map[path] = lockSlim;
        }

        return lockSlim;
    }
}