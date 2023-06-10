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
namespace zcfux.Telemetry.Node;

sealed class PendingCommands
{
    static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    readonly object _lock = new();
    readonly HashSet<PendingCommand> _commands = new();
    readonly SemaphoreSlim _semaphore = new(0);

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _commands.Count;
            }
        }
    }

    public void Put(Task task, Type returnType, string? responseTopic, int? messageId)
        => Put(new PendingCommand(
            task,
            returnType,
            responseTopic,
            messageId));

    void Put(PendingCommand pendingTask)
    {
        lock (_lock)
        {
            _commands.Add(pendingTask);
        }

        _semaphore.Release();
    }

    public async Task<PendingCommand[]> WaitAsync(CancellationToken cancellationToken)
    {
        var pendingCommands = Array.Empty<PendingCommand>();

        while (!pendingCommands.Any())
        {
            var snapshot = GetSnapshot();

            if (snapshot.Any())
            {
                var tasks = new List<Task>(snapshot.Keys);

                var timeoutTask = Task.Delay(DefaultTimeout, cancellationToken);

                tasks.Add(timeoutTask);

                var winner = await Task.WhenAny(tasks);

                if (snapshot.TryGetValue(winner, out var completedCommands))
                {
                    pendingCommands = RemovePendingTasks(completedCommands);
                }
            }
            else
            {
                await _semaphore.WaitAsync(cancellationToken);
            }
        }

        return pendingCommands;
    }

    IDictionary<Task, PendingCommand[]> GetSnapshot()
    {
        lock (_lock)
        {
            return _commands
                .GroupBy(p => p.Task)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }
    }

    PendingCommand[] RemovePendingTasks(IEnumerable<PendingCommand> pendingCommands)
    {
        lock (_lock)
        {
            return pendingCommands
                .Where(pendingCommand => _commands.Remove(pendingCommand))
                .ToArray();
        }
    }
}