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
using LinqToDB;
using zcfux.Audit.LinqToDB;
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Translation.Data;
using zcfux.Translation.LinqtoDB;
using zcfux.Translation.LinqToDB;

namespace zcfux.Audit.Test.LinqToDB;

sealed class Methods : IDbTestMethods
{
    readonly string _connectionString;

    public Methods(string connectionString)
        => _connectionString = connectionString;

    public IEngine NewEngine()
    {
        var opts = new DataOptions()
            .UseSQLite(_connectionString);

        return new Engine(opts);
    }

    public void DeleteAll(object handle)
    {
        var db = (handle as Handle)!.Db();

        db.GetTable<TopicRelation>().Delete();
        db.GetTable<TopicKindRelation>().Delete();
        db.GetTable<AssociationRelation>().Delete();
        db.GetTable<TopicAssociationRelation>().Delete();
        db.GetTable<LocaleRelation>().Delete();
        db.GetTable<TextCategoryRelation>().Delete();
    }

    public IAuditDb CreateAuditDb()
        => new AuditDb();
    
    public ITranslationDb CreateTranslationDb()
        => new TranslationDb();
}