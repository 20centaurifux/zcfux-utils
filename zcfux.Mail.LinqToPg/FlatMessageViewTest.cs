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
using zcfux.Filter;

namespace zcfux.Mail.LinqToPg;

sealed class FlatMessageViewTest : IVisitor
{
    public static bool ColumnRequiresFlatView(string columName)
        => (columName is "To" or "Cc" or "Bcc" or "AttachmentId" or "Attachment");

    public bool RequiresView { get; private set; }

    public static bool UseFlatView(Query query)
    {
        var useView = false;

        if (query.HasFilter)
        {
            var visitor = new FlatMessageViewTest();

            query.Filter?.Traverse(visitor);

            useView = visitor.RequiresView;
        }

        if (query.Order.Any(t => FlatMessageViewTest.ColumnRequiresFlatView(t.Item1)))
        {
            useView = true;
        }

        return useView;
    }

    public void BeginFunction(string name)
    {
    }

    public void EndFunction()
    {
    }

    public void Visit(object? value)
    {
        if (value is IColumn column)
        {
            if (ColumnRequiresFlatView(column.Name))
            {
                RequiresView = true;
            }
        }
    }
}