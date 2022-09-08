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
using zcfux.Application;

namespace zcfux.User.LinqToDB;

[Table(Schema = "user", Name = "AssignedUsers")]
internal class AssignedUserView : IAssignedUser
{
#pragma warning disable CS8618
    [Column(Name = "SiteUid")]
    public Guid SiteUid { get; set; }

    [Column(Name = "Site")]
    public string Site { get; set; }

    [Column(Name = "OriginId")]
    public int OriginId { get; set; }

    [Column(Name = "Origin")]
    public string Origin { get; set; }

    [Column(Name = "Writable")]
    public bool Writable { get; set; }

    [Column(Name = "UserUid")]
    public Guid UserUid { get; set; }

    [Column(Name = "Username")]
    public string Username { get; set; }

    [Column(Name = "Status")]
    public EStatus Status { get; set; }

    [Column(Name = "Firstname")]
    public string? Firstname { get; set; }

    [Column(Name = "Lastname")]
    public string? Lastname { get; set; }

    [Column(Name = "GroupUid")]
    public Guid GroupUid { get; set; }

    [Column(Name = "Group"), NotNull]
    public string Group { get; set; }

    ISite IAssignedUser.Site
        => new Site(SiteUid, Site);

    IGroup IAssignedUser.Group
        => new GroupRelation
        {
            Guid = GroupUid,
            Name = Group
        };

    IUser IAssignedUser.User
        => new UserRelation
        {
            Guid = UserUid,
            Name = Username,
            Firstname = Firstname,
            Lastname = Lastname,
            Status = Status,
            OriginId = OriginId,
            Origin = new OriginRelation
            {
                Id = OriginId,
                Name = Origin,
                Writable = Writable
            }
        };
#pragma warning restore CS8618
}