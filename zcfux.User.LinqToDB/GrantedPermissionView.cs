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

[Table(Schema = "user", Name = "GrantedPermissions")]
internal class GrantedPermissionView
{
#pragma warning disable CS8618
    [Column(Name = "GroupUid")]
    public Guid GroupUid { get; set; }

    [Column(Name = "Group")]
    public string Group { get; set; }

    [Column(Name = "Id")]
    public int Id { get; set; }

    [Column(Name = "Name")]
    public string Name { get; set; }

    [Column(Name = "CategoryId")]
    public int CategoryId { get; set; }

    [Column(Name = "Category")]
    public string Category { get; set; }

    [Column(Name = "ApplicationId")]
    public int ApplicationId { get; set; }

    [Column(Name = "Application")]
    public string Application { get; set; }
#pragma warning restore CS8618
}