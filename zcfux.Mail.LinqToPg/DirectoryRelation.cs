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

namespace zcfux.Mail;

[Table(Schema = "mail", Name = "Directory")]
internal sealed class DirectoryRelation : IDirectory
{
#pragma warning disable CS8618

    public DirectoryRelation()
    {
    }

    public DirectoryRelation(IDirectory directory)
    {
        Id = directory.Id;
        Name = directory.Name;

        if (directory.Parent != null)
        {
            ParentId = directory.Parent.Id;
            Parent = new DirectoryRelation(directory.Parent);
        }
    }

    [Column(Name = "Id", IsPrimaryKey = true, IsIdentity = true)]
    public int? Id { get; set; }

    int IDirectory.Id => Id!.Value;

    [Column(Name = "Name", CanBeNull = false)]
    public string Name { get; set; }

    string IDirectory.Name => Name;

    [Column(Name = "ParentId")]
    public int? ParentId { get; set; }

    [Association(ThisKey = "ParentId", OtherKey = "Id")]
    public DirectoryRelation? Parent { get; set; }

    IDirectory? IDirectory.Parent => Parent;
#pragma warning restore CS8618
}