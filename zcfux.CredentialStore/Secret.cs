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

public sealed class Secret
{
    readonly Dictionary<string, string> _data;
    private readonly DateTime? _expiryDate;

    public Secret(IReadOnlyDictionary<string, string> data, DateTime? expiryDate)
        => (_data, _expiryDate) = (new Dictionary<string, string>(data), expiryDate);

    public DateTime? ExpiryDate
        => _expiryDate;

    public IReadOnlyDictionary<string, string> Data
        => _data;

    public bool IsExpired()
        => IsExpired(DateTime.UtcNow);

    public bool IsExpired(DateTime date)
        => _expiryDate.HasValue
           && (_expiryDate.Value < date);

    public override bool Equals(object? obj)
    {
        var equals = (obj is Secret other
                      && _expiryDate.Equals(other._expiryDate)
                      && _data.Count.Equals(other._data.Count)
                      && !_data.Except(other._data).Any());

        return equals;
    }

    public override int GetHashCode()
        => _data.GetHashCode();
}