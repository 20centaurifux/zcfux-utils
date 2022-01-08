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
using System.Runtime.CompilerServices;

namespace zcfux.Filter;

public sealed record Column<T> : IColumn, INode
{
    public string Name { get; }

    public Type Type { get; } = typeof(T);

    Column(string name)
        => Name = name;

    public static implicit operator Column<T>(string name)
        => new(name);

    public static Column<T> FromMember([CallerMemberName] string name = default!)
        => new(name);

    public Column<T> Rename(string name) => new(name);

    public void Traverse(IVisitor visitor)
        => visitor.Visit(this);
}