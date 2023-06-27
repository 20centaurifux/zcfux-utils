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

public interface IConnection : IAsyncDisposable
{
    event Func<EventArgs, Task>? ConnectedAsync;
    event Func<EventArgs, Task>? DisconnectedAsync;
    event Func<ApiInfoEventArgs, Task>? ApiInfoReceivedAsync;
    event Func<ApiMessageEventArgs, Task>? ApiMessageReceivedAsync;
    event Func<NodeStatusEventArgs, Task>? StatusReceivedAsync;
    event Func<ResponseEventArgs, Task>? ResponseReceivedAsync;

    bool IsConnected { get; }

    string ClientId { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task SubscribeToStatusAsync(NodeFilter filter, CancellationToken cancellationToken = default);

    Task SendStatusAsync(NodeStatusMessage message, CancellationToken cancellationToken = default);

    Task SubscribeToApiInfoAsync(NodeFilter filter, CancellationToken cancellationToken = default);

    Task SendApiInfoAsync(ApiInfoMessage message, CancellationToken cancellationToken = default);

    Task SubscribeToApiMessagesAsync(ApiFilter filter, EDirection direction, CancellationToken cancellationToken = default);

    Task SendApiMessageAsync(ApiMessage message, CancellationToken cancellationToken = default);

    Task SubscribeResponseAsync(NodeDetails node, CancellationToken cancellationToken = default);

    Task SendResponseAsync(ResponseMessage message, CancellationToken cancellationToken = default);
}