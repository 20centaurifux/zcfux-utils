﻿/***************************************************************************
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

[Table(Schema = "user", Name = "PermissionCategories")]
internal class PermissionCategoryView
{
#pragma warning disable CS8618
    [Column(Name = "Id"), PrimaryKey]
    public int Id { get; set; }

    [Column(Name = "Name"), NotNull]
    public string Name { get; set; }

    [Column(Name = "ApplicationId"), NotNull]
    public int ApplicationId { get; set; }

    [Column(Name = "Application"), NotNull]
    public string Application { get; set; }

    public IPermissionCategory ToPermissionCategory()
        => new PermissionCategory(
            Id,
            Name,
            new Application(
                ApplicationId,
                Application));
#pragma warning restore CS8618
}