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

internal sealed class ConvertHandleInterceptor<TInterface, TImpl> : IInterceptor
{
    static readonly Lazy<ConcurrentDictionary<string, MethodInfo>> Methods
        = new(() => new ConcurrentDictionary<string, MethodInfo>());

    readonly object _impl;

    public ConvertHandleInterceptor()
    {
        var impl = Activator.CreateInstance(typeof(TImpl))!;

        UpdateMapping(impl);

        _impl = impl;
    }

    static void UpdateMapping(object impl)
    {
        if (!Methods.IsValueCreated)
        {
            var implType = impl.GetType();
            var mapping = Methods.Value;

            foreach (var method in typeof(TInterface).GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var parameterTypes = method.GetParameters()
                    .Select(p => p.ParameterType)
                    .Skip(1)
                    .ToArray();

                var targetMethod = FindTargetMethod(implType, method.Name, parameterTypes);

                var key = ToKey(method.Name, parameterTypes);

                mapping[key] = targetMethod;
            }
        }
    }

    static MethodInfo FindTargetMethod(IReflect type, string name, Type[] parameterTypes)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == name);

        foreach (var method in methods)
        {
            var methodParameterTypes = method.GetParameters()
                .Select(p => p.ParameterType)
                .Skip(1)
                .ToArray();

            if (parameterTypes.SequenceEqual(methodParameterTypes))
            {
                return method;
            }
        }

        throw new MissingMethodException($"Method not found.", name);
    }

    public void Intercept(IInvocation invocation)
    {
        var method = MapMethod(invocation.Method.Name, invocation.Arguments)
                     ?? throw new NotImplementedException();

        var returnValue = method.Invoke(_impl, invocation.Arguments);

        invocation.ReturnValue = returnValue;
    }

    static MethodInfo? MapMethod(string name, object[] arguments)
    {
        var argumentTypes = arguments
            .Skip(1)
            .Select(arg => arg.GetType())
            .ToArray();

        var key = ToKey(name, argumentTypes);

        Methods.Value.TryGetValue(key, out var methodInfo);

        return methodInfo;
    }

    static string ToKey(string name, Type[] types)
        => $"{name}_{string.Join('_', types.Cast<object>())}";
}