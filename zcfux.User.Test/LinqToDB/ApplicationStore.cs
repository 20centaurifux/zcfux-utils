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
using zcfux.Application;
using zcfux.Data.LinqToDB;
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.User.Test.LinqToDB;

internal class ApplicationStore
    : ISiteDb
    , IApplicationDb
{
    public void Write(object handle, ISite site)
    {
        var relation = new SiteRelation(site);

        (handle as Handle)!
            .Db()
            .Insert(relation);
    }

    public void Delete(object handle, Guid guid)
        => (handle as Handle)!
                .Db()
                .GetTable<SiteRelation>()
                .Where(s => s.Guid == guid)
                .Delete();

    public IEnumerable<ISite> Query(object handle, Query query)
        => (handle as Handle)!
                .Db()
                .GetTable<SiteRelation>()
                .Query(query);

    public void Write(object handle, IApplication application)
    {
        var relation = new ApplicationRelation(application);

        (handle as Handle)!
            .Db()
            .Insert(relation);
    }

    public void Delete(object handle, int id)
        => (handle as Handle)!
                .Db()
                .GetTable<ApplicationRelation>()
                .Where(a => a.Id == id)
                .Delete();

    IEnumerable<IApplication> IApplicationDb.Query(object handle, Query query)
        => (handle as Handle)!
                .Db()
                .GetTable<ApplicationRelation>()
                .Query(query);
}