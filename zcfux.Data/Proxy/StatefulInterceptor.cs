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
using System.Reflection;
using Castle.DynamicProxy;

namespace zcfux.Data.Proxy;

internal sealed class StatefulInterceptor : IInterceptor
{
    readonly object _handle;
    readonly object _impl;

    public StatefulInterceptor(object handle, object impl)
        => (_handle, _impl) = (handle, impl);

    public void Intercept(IInvocation invocation)
    {
        if (!InvokeWithoutInjection(invocation))
        {
            InvokeWithInjection(invocation);
        }
    }

    bool InvokeWithoutInjection(IInvocation invocation)
    {
        var invoked = false;

        var types = invocation.Arguments
            .Select(arg => arg.GetType())
            .ToArray();

        var method = _impl.GetType()
            .GetMethod(invocation.Method.Name, BindingFlags.Public | BindingFlags.Instance, types);

        if (method != null
            && method.GetCustomAttribute(typeof(DisableHandleInjectionAttribute)) != null)
        {
            invocation.ReturnValue = method.Invoke(_impl, invocation.Arguments);

            invoked = true;
        }

        return invoked;
    }

    void InvokeWithInjection(IInvocation invocation)
    {
        var arguments = new List<object>
        {
            _handle
        };

        arguments.AddRange(invocation.Arguments);

        var types = arguments
            .Select(arg => arg.GetType())
            .ToArray();

        var method = _impl.GetType()
            .GetMethod(invocation.Method.Name, BindingFlags.Public | BindingFlags.Instance, types);

        invocation.ReturnValue = method!.Invoke(_impl, arguments.ToArray());
    }
}