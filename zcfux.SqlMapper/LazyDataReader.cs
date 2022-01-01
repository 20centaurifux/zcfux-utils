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
using System.Collections;
using System.Data;

namespace zcfux.SqlMapper;

internal sealed class LazyDataReader<T> : IEnumerable<T> where T : new()
{
    readonly IDataReader _reader;

    public LazyDataReader(IDataReader reader)
        => _reader = reader;

    public IEnumerator<T> GetEnumerator()
    {
        while (true)
        {
            var obj = _reader.ReadAndMap<T>();

            if (obj == null)
            {
                break;
            }

            yield return obj;
        }

        _reader.Close();
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}