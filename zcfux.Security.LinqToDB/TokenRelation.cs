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
using zcfux.Security.Token;

namespace zcfux.Security.LinqToDB;

[Table(Schema = "security", Name = "Token")]
internal class TokenRelation : IToken
{
#pragma warning disable CS8618
    public TokenRelation()
    {
    }

    public TokenRelation(IToken other)
    {
        Value = other.Value;
        KindId = other.Kind.Id;
        Kind = new TokenKindRelation(other.Kind);
        Counter = other.Counter;
        EndOfLife = other.EndOfLife;
    }

    [Column(Name = "Value"), PrimaryKey]
    public string Value { get; set; }

    [Column(Name = "KindId"), PrimaryKey]
    public int KindId { get; set; }

    [Association(ThisKey = "KindId", OtherKey = "Id")]
    public TokenKindRelation Kind { get; set; }

    IKind IToken.Kind => Kind;

    [Column(Name = "Counter"), NotNull]
    public int Counter { get; set; }

    [Column(Name = "EndOfLife")]
    public DateTime? EndOfLife { get; set; }
#pragma warning restore CS8618
}