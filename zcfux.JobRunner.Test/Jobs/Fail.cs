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
using System.Collections.Concurrent;

namespace zcfux.JobRunner.Test.Jobs;

public sealed class Fail : ARegularJob
{
    static readonly ConcurrentDictionary<Guid, long> State = new();

    public static long Fails = 1;

    long _counter;

    protected override void Action()
    {
        if (_counter <= Fails)
        {
            ++_counter;

            throw new Exception();
        }
    }

    public override void Freeze()
    {
        base.Freeze();

        State[Guid] = _counter;
    }

    protected override void Restore()
    {
        base.Restore();

        _counter = State[Guid];
    }
}