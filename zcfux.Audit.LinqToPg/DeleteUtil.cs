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
using LinqToDB.Data;
using zcfux.Filter;
using zcfux.Filter.Linq;

namespace zcfux.Audit.LinqToPg;

sealed class DeleteUtil
{
    readonly string _relationPrefix;

    public DeleteUtil(string relationPrefix)
        => _relationPrefix = relationPrefix;

    public void Delete(DataConnection db, INode filter)
    {
        var events = QueryEvents(db, filter);

        var topicAssociations = FindTopicAssociations(db, events);

        DeleteTopicAssociations(db, topicAssociations);
        DeleteEvents(db, events);
        DeleteOrphans(db);

        events.Drop();
        topicAssociations.Drop();
    }

    TempTable<EventRelation> QueryEvents(DataConnection db, INode filter)
    {
        var expr = filter.ToExpression<EventView>();

        return (from ev in db.GetTable<EventRelation>()
                where db.GetTable<EventView>()
                    .SchemaName("audit")
                    .TableName(PrependPrefix("EventView"))
                    .Where(expr)
                    .Any(v => v.Id == ev.Id)
                select ev)
            .IntoTempTable("_event");
    }

    TempTable<TopicAssociationRelation> FindTopicAssociations(DataConnection db, IQueryable<EventRelation> events)
    {
        var queryable = db.GetCte<TopicAssociationRelation>(cte =>
        {
            return (
                    from e in events
                    from ta in db.GetTable<TopicAssociationRelation>()
                        .TableName(PrependPrefix("TopicAssociation"))
                        .InnerJoin(a => a.Topic1 == e.TopicId)
                    select ta
                )
                .Concat(
                    from ta in db.GetTable<TopicAssociationRelation>()
                        .TableName(PrependPrefix("TopicAssociation"))
                    from p in cte.InnerJoin(x => x.Topic2 == ta.Topic1)
                    select ta
                );
        });

        return queryable.IntoTempTable("_topic_association");
    }

    void DeleteTopicAssociations(DataConnection db, IQueryable<TopicAssociationRelation> topicAssociations)
    {
        var queryable = from ta in db.GetTable<TopicAssociationRelation>()
                .TableName(PrependPrefix("TopicAssociation"))
            where topicAssociations.Any(a => a.Id == ta.Id)
            select ta;

        queryable.Delete();
    }

    void DeleteEvents(DataConnection db, IQueryable<EventRelation> events)
    {
        var queryable = from ev in db.GetTable<EventRelation>()
                .TableName(PrependPrefix("Event"))
            where events.Any(e => e.Id == ev.Id)
            select ev;

        queryable.Delete();
    }

    void DeleteOrphans(DataConnection db)
    {
        var queryable = from t in db.GetTable<TopicRelation>()
            where !db.GetTable<TopicAssociationRelation>()
                .Any(a => a.Topic1 == t.Id || a.Topic2 == t.Id)
            select t;

        queryable.Delete();
    }

    string PrependPrefix(string tableName)
        => string.Concat(_relationPrefix, tableName);
}