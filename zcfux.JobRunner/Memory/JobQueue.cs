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
namespace zcfux.JobRunner.Memory;

public sealed class JobQueue : AJobQueue
{
    readonly object _lock = new();
    readonly PriorityQueue<AJob, DateTime?> _queue = new();

    protected override bool TryPeek(out AJob? job)
    {
        lock (_lock)
        {
            return _queue.TryPeek(out job, out _);
        }
    }

    protected override AJob Dequeue()
    {
        lock (_lock)
        {
            return _queue.Dequeue();
        }
    }

    protected override void Enqueue(AJob job)
    {
        lock (_lock)
        {
            _queue.Enqueue(job, job.NextDue);
        }
    }
}