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
using NCrontab;

namespace zcfux.JobRunner;

public abstract class ACronJob : AJob
{
    static readonly CrontabSchedule.ParseOptions CronParseOptions = new()
    {
        IncludingSeconds = true
    };

    CrontabSchedule? _schedule;

    internal void ParseExpression(string expression)
    {
        _schedule = CrontabSchedule.Parse(expression, CronParseOptions);

        NextDue = Schedule();
    }

    protected override DateTime? Schedule()
        => _schedule?.GetNextOccurrence(LastDone ?? DateTime.UtcNow);
}