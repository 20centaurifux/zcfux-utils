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
using LinqToDB;
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;
using zcfux.Security.Token;

namespace zcfux.Security.LinqToDB;

public sealed class TokenDb : ITokenDb
{
    public void WriteKind(object handle, IKind kind)
    {
        var db = handle.Db();

        if (db
            .GetTable<TokenKindRelation>()
            .Any(k => !k.Id.Equals(kind.Id) && k.Name.Equals(kind.Name)))
        {
            throw new ConflictException();
        }

        var relation = new TokenKindRelation(kind);

        db.InsertOrReplace(relation);
    }

    public void DeleteKind(object handle, int id)
    {
        var deleted = handle
            .Db()
            .GetTable<TokenKindRelation>()
            .Where(k => k.Id == id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public IEnumerable<IKind> QueryKinds(object handle, Query query)
        => handle
            .Db()
            .GetTable<TokenKindRelation>()
            .Query(query);

    public void WriteToken(object handle, IToken token)
    {
        var db = handle.Db();

        if (!db
                .GetTable<TokenKindRelation>()
                .Any(k => k.Id.Equals(token.Kind.Id)))
        {
            throw new NotFoundException($"Kind (id={token.Kind.Id}) not found.");
        }

        var relation = new TokenRelation(token);

        db.InsertOrReplace(relation);
    }

    public void DeleteToken(object handle, int kindId, string value)
    {
        var deleted = handle
            .Db()
            .GetTable<TokenRelation>()
            .Where(t => (t.KindId == kindId) && (t.Value == value))
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public int DeleteTokens(object handle, INode filter)
    {
        var db = handle.Db();

        var expr = filter.ToExpression<TokenView>();

        var tokens = db
            .GetTable<TokenView>()
            .Where(expr);

        var deleted = db
            .GetTable<TokenRelation>()
            .Where(t1 => tokens.Any(t2 => t2.Value == t1.Value && t2.KindId == t1.KindId))
            .Delete();

        return deleted;
    }

    public IEnumerable<IToken> QueryTokens(object handle, Query query)
        => handle
            .Db()
            .GetTable<TokenView>()
            .Query(query)
            .Select(t => t.ToToken());
}