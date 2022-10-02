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
namespace zcfux.Telemetry;

public interface IConnection : IDisposable
{
    event EventHandler? Connected;
    event EventHandler? Disconnected;
    event EventHandler<ApiInfoEventArgs>? ApiInfoReceived;
    event EventHandler<ApiMessageEventArgs>? ApiMessageReceived;
    event EventHandler<DeviceStatusEventArgs>? DeviceStatusReceived;

    bool IsConnected { get; }

    string ClientId { get; }

    Task ConnectAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);

    Task SubscribeToDeviceStatusAsync(DeviceFilter filter, CancellationToken cancellationToken);

    Task SendDeviceStatusAsync(DeviceStatusMessage message, CancellationToken cancellationToken);
    
    Task SubscribeToApiInfoAsync(ApiFilter filter, CancellationToken cancellationToken);

    Task SendApiInfoAsync(ApiInfoMessage message, CancellationToken cancellationToken);

    Task SubscribeToApiMessagesAsync(DeviceDetails device, string api, EDirection direction, CancellationToken cancellationToken);

    Task SendApiMessageAsync(ApiMessage message, CancellationToken cancellationToken);
}