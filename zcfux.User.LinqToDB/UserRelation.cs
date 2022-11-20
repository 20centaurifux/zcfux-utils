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
using LinqToDB.Mapping;

namespace zcfux.User.LinqToDB;

[Table(Schema = "user", Name = "User")]
sealed class UserRelation : IUser
{
#pragma warning disable CS8618
    public UserRelation()
    {
    }

    public UserRelation(IUser other)
    {
        Guid = other.Guid;
        OriginId = other.Origin.Id;

        Origin = new OriginRelation
        {
            Id = other.Origin.Id,
            Name = other.Origin.Name,
            Writable = other.Origin.Writable
        };

        Name = other.Name;
        Firstname = other.Firstname;
        Lastname = other.Lastname;
        Status = other.Status;
    }

    [Column(Name = "Guid"), PrimaryKey]
    public Guid Guid { get; set; }

    [Column(Name = "OriginId"), NotNull]
    public int OriginId { get; set; }

    [Association(ThisKey = "OriginId", OtherKey = "Id")]
    public OriginRelation Origin { get; set; }

    IOrigin IUser.Origin => Origin;

    [Column(Name = "Name"), NotNull]
    public string Name { get; set; }

    [Column(Name = "Status"), NotNull]
    public EStatus Status { get; set; }

    [Column(Name = "Firstname")]
    public string? Firstname { get; set; }

    [Column(Name = "Lastname")]
    public string? Lastname { get; set; }
#pragma warning restore CS8618
}