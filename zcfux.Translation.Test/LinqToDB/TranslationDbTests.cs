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
using LinqToDB.Configuration;
using zcfux.Data;
using zcfux.Data.LinqToDB;
using zcfux.Translation.Data;
using zcfux.Translation.LinqtoDB;

namespace zcfux.Translation.Test.LinqToDB;

public sealed class TranslationDbTests : ATranslationDbTests
{
    protected override IEngine CreateAndSetupEngine()
    {
        var db = new TestDb();

        db.Create();

        var builder = new LinqToDBConnectionOptionsBuilder();

        builder.UseSQLite(db.ConnectionString!);

        var opts = builder.Build();

        var engine = new Engine(opts);

        engine.Setup();

        return engine;
    }

    protected override ITranslationDb CreateTranslationDb()
        => new TranslationDb();
}