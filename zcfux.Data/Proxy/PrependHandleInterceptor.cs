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
    Lesser General Public License for more detail

    You should have received a copy of the GNU Lesser General Public License
    along with this program; if not, write to the Free Software Foundation,
    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 ***************************************************************************/
using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;

namespace zcfux.Data.Proxy;

sealed class PrependHandleInterceptor<TImpl>
    : IInterceptor
    where TImpl : class
{
    static readonly Lazy<ConcurrentDictionary<string, MethodInfo>> Methods
        = new(() => new ConcurrentDictionary<string, MethodInfo>());

    readonly TImpl _impl;
    readonly object _handle;

    public PrependHandleInterceptor(TImpl impl, object handle)
        => (_handle, _impl) = (handle, impl);

    public void Intercept(IInvocation invocation)
    {
        var argumentTypes = invocation.Method.GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();

        var key = ToKey(invocation.Method.Name, argumentTypes);

        var mapping = Methods.Value;

        if (!mapping.TryGetValue(key, out var method))
        {
            var types = new List<Type>
            {
                _handle.GetType()
            };

            types.AddRange(argumentTypes);

            method = typeof(TImpl)
                         .GetMethod(invocation.Method.Name, BindingFlags.Public | BindingFlags.Instance, types.ToArray())
                     ?? throw new NotImplementedException();

            Methods.Value[key] = method;
        }

        var arguments = new List<object>
        {
            _handle
        };

        arguments.AddRange(invocation.Arguments);

        try
        {
            invocation.ReturnValue = method!.Invoke(_impl, arguments.ToArray());
        }
        catch (TargetInvocationException ex) when (ex.InnerException is { })
        {
            throw ex.InnerException;
        }
    }

    static string ToKey(string name, Type[] types)
        => $"{name}_{string.Join('_', types.Cast<object>())}";
}