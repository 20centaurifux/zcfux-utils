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
using System.Text;
using Newtonsoft.Json;

namespace zcfux.Telemetry.MQTT;

public sealed class Serializer : ISerializer
{
    public byte[] Serialize(object? value)
    {
        var json = JsonConvert.SerializeObject(value);

        return Encoding.UTF8.GetBytes(json);
    }

    public T? Deserialize<T>(byte[] data)
    {
        var json = Encoding.UTF8.GetString(data);

        return JsonConvert.DeserializeObject<T>(json);
    }

    public object? Deserialize(byte[] data, Type type)
    {
        var json = Encoding.UTF8.GetString(data);

        return JsonConvert.DeserializeObject(json, type);
    }
}