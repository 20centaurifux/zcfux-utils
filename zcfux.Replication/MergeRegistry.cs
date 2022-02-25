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
using zcfux.Replication.Generic;

namespace zcfux.Replication;

public sealed class MergeRegistry
{
    readonly Dictionary<Type, object> _m = new Dictionary<Type, object>();
    bool _built = false;

    public void Register<T>(IMerge<T> merge)
        where T : IEntity
    {
        if (_built)
        {
            throw new InvalidOperationException("Registry has been built.");
        }

        _m[typeof(T)] = merge;
    }

    public void Build()
        => _built = true;

    public IMerge<T> Get<T>()
        where T : IEntity
    {
        if (!_built)
        {
            throw new InvalidOperationException("Registry has not been built.");
        }

        return (_m[typeof(T)] as IMerge<T>)!;
    }

    public IMerge Get(Type type)
    {
        if (!_built)
        {
            throw new InvalidOperationException("Registry has not been built.");
        }

        return (_m[type] as IMerge)!;
    }
}