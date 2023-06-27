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
namespace zcfux.Telemetry.Test.Discovery;

sealed class PowerImpl_V1_1 : IPowerApi_V1_1
{
    readonly Producer<bool> _producer = new();
    readonly object _lock = new();
    bool _value;

    public IAsyncEnumerable<bool> Power => _producer;

    public Task<bool> ToggleAsync()
    {
        bool newState;

        lock (_lock)
        {
            newState = !_value;
            _value = newState;
        }

        _producer.Write(newState);

        return Task.FromResult(newState);
    }

    public Task OffAsync()
    {
        bool changed = true;

        lock (_lock)
        {
            if(_value)
            {
                _value = false;
                changed = true;
            }
        }

        if(changed)
        {
            _producer.Write(false);
        }

        return Task.CompletedTask;
    }

    public Task OnAsync()
    {
        bool changed = true;

        lock (_lock)
        {
            if (!_value)
            {
                _value = true;
                changed = true;
            }
        }

        if (changed)
        {
            _producer.Write(true);
        }

        return Task.CompletedTask;
    }
}