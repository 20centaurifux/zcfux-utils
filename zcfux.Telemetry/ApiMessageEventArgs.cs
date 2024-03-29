﻿/***************************************************************************
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

public sealed class ApiMessageEventArgs : EventArgs
{
    public NodeDetails Node { get; }

    public string Api { get; }

    public string Topic { get; }

    public byte[] Payload { get; }

    public EDirection Direction { get; }

    public TimeSpan TimeToLive { get; }

    public string? ResponseTopic { get; }

    public int? MessageId { get; }

    public ApiMessageEventArgs(
        NodeDetails node,
        string api,
        string topic,
        byte[] payload,
        EDirection direction,
        TimeSpan timeToLive,
        string? responseTopic,
        int? messageId)
        => (Node, Api, Topic, Payload, Direction, TimeToLive, ResponseTopic, MessageId)
            = (node, api, topic, payload, direction, timeToLive, responseTopic, messageId);
}