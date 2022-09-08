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

[Table(Schema = "user", Name = "PermissionDependencies")]
internal class PermissionDependencyView
{
#pragma warning disable CS8618
    [Column(Name = "ApplicationId")]
    public int LeftApplicationId { get; set; }

    [Column(Name = "Application")]
    public string LeftApplication { get; set; }

    [Column(Name = "CategoryId")]
    public int LeftCategoryId { get; set; }

    [Column(Name = "Category")]
    public string LeftCategory { get; set; }

    [Column(Name = "PermissionId")]
    public int LeftId { get; set; }

    [Column(Name = "Permission")]
    public string LeftPermission { get; set; }

    [Column(Name = "Dep_ApplicationId")]
    public int RightApplicationId { get; set; }

    [Column(Name = "Dep_Application")]
    public string RightApplication { get; set; }

    [Column(Name = "Dep_CategoryId")]
    public int RightCategoryId { get; set; }

    [Column(Name = "Dep_Category")]
    public string RightCategory { get; set; }

    [Column(Name = "Dep_PermissionId")]
    public int RightId { get; set; }

    [Column(Name = "Dep_Permission")]
    public string RightPermission { get; set; }
#pragma warning restore CS8618
}