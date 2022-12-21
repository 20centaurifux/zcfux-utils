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
using Castle.Core.Resource;
using LinqToDB;
using LinqToDB.Data;
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Translation.Data;
using zcfux.Translation.LinqToDB;

namespace zcfux.Translation.LinqtoDB;

public sealed class TranslationDb : ITranslationDb
{
    public void WriteCategory(object handle, ICategory category)
    {
        var db = handle.Db();

        var relation = new TextCategoryRelation
        {
            Id = category.Id,
            Name = category.Name
        };

        db.InsertOrReplace(relation);
    }

    public IEnumerable<ICategory> GetCategories(object handle)
        => handle
            .Db()
            .GetTable<TextCategoryRelation>();

    public void DeleteCategory(object handle, int id)
    {
        var deleted = handle
            .Db()
            .GetTable<TextCategoryRelation>()
            .Where(c => c.Id == id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public void WriteLocale(object handle, ILocale locale)
    {
        var db = handle.Db();

        var relation = new LocaleRelation
        {
            Id = locale.Id,
            Name = locale.Name
        };

        db.InsertOrReplace(relation);
    }

    public IEnumerable<ILocale> GetLocales(object handle)
        => handle
            .Db()
            .GetTable<LocaleRelation>();

    public void DeleteLocale(object handle, int id)
    {
        var deleted = handle
            .Db()
            .GetTable<LocaleRelation>()
            .Where(l => l.Id == id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public ITextResource NewTextResource(object handle, ICategory category, string msgId)
    {
        var db = handle.Db();

        if (!db
                .GetTable<TextCategoryRelation>()
                .Any(c => c.Id.Equals(category.Id)))
        {
            throw new NotFoundException($"Category (id=`{category.Id}') not found.");
        }

        if (db
            .GetTable<TextResourceRelation>()
            .Any(r =>
                r.CategoryId.Equals(category.Id)
                && r.MsgId.Equals(msgId)))
        {
            throw new ConflictException();
        }

        var relation = new TextResourceRelation
        {
            CategoryId = category.Id,
            Category = new TextCategoryRelation
            {
                Id = category.Id,
                Name = category.Name
            },
            MsgId = msgId
        };

        relation.Id = db.InsertWithInt32Identity(relation);

        return relation;
    }

    public ITextResource? TryGetTextResource(object handle, ICategory category, string msgId)
        => handle
            .Db()
            .GetTable<TextResourceRelation>()
            .LoadWith(t => t.Category)
            .SingleOrDefault(t => t.CategoryId.Equals(category.Id) && t.MsgId.Equals(msgId));

    public ITextResource GetOrCreateTextResource(object handle, ICategory category, string msgId)
    {
        var db = handle.Db();

        if (!db
                .GetTable<TextCategoryRelation>()
                .Any(c => c.Id.Equals(category.Id)))
        {
            throw new NotFoundException($"Category (id=`{category.Id}') not found.");
        }

        var relation = db
            .GetTable<TextResourceRelation>()
            .LoadWith(r => r.Category)
            .SingleOrDefault(r =>
                r.CategoryId.Equals(category.Id)
                && r.MsgId.Equals(msgId));

        if (relation is null)
        {
            relation = new TextResourceRelation
            {
                CategoryId = category.Id,
                Category = new TextCategoryRelation
                {
                    Id = category.Id,
                    Name = category.Name
                },
                MsgId = msgId
            };

            relation.Id = db.InsertWithInt32Identity(relation);
        }

        ;

        return relation;
    }

    public void DeleteTextResource(object handle, int id)
    {
        var deleted = handle
            .Db()
            .GetTable<TextResourceRelation>()
            .Where(r => r.Id == id)
            .Delete();

        if (deleted == 0)
        {
            throw new NotFoundException();
        }
    }

    public void LocalizeText(object handle, ITextResource resource, ILocale locale, string translation)
    {
        var db = handle.Db();

        if (!db
                .GetTable<LocaleRelation>()
                .Any(l => l.Id.Equals(locale.Id)))
        {
            throw new NotFoundException($"Locale (id=`{locale.Id}') not found.");
        }

        var relation = new TranslatedTextRelation
        {
            ResourceId = resource.Id,
            LocaleId = locale.Id,
            Translation = translation
        };

        db.InsertOrReplace(relation);
    }

    public IEnumerable<KeyValuePair<ITextResource, string>> GetTranslation(object handle, string locale)
    {
        var translations = handle
            .Db()
            .GetTable<TranslatedTextRelation>()
            .LoadWith(t => t.Resource)
            .ThenLoad(r => r.Category)
            .LoadWith(t => t.Locale)
            .Where(t => t.Locale.Name.Equals(locale));

        foreach (var t in translations)
        {
            yield return new KeyValuePair<ITextResource, string>(
                t.Resource,
                t.Translation);
        }
    }
}