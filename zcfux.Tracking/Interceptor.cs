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
using Castle.DynamicProxy;

namespace zcfux.Tracking;

internal sealed class Interceptor : IInterceptor
{
    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method.Name;

        if (method.StartsWith("set_"))
        {
            var propertyName = method.Substring(4);

            var prop = invocation.TargetType
                .GetProperties()
                .Single(prop => prop.Name == propertyName);

            var attr = prop.GetCustomAttributes(typeof(TrackableAttribute), true)
                .Cast<TrackableAttribute>()
                .SingleOrDefault();

            if (attr is { })
            {
                PropertyChanged(invocation, propertyName, attr);
            }
        }

        invocation.Proceed();
    }

    void PropertyChanged(IInvocation invocation, string propertyName, TrackableAttribute attr)
    {
        if (invocation.Proxy is IChangedProperties obj)
        {
            var value = WrapValue(invocation.Arguments[0]);

            invocation.Arguments[0] = value;

            obj.ChangeProperty(propertyName, value);
        }
    }

    object? WrapValue(object? value)
    {
        if (!ProxyUtil.IsProxy(value))
        {
            if (value is ATrackable trackable)
            {
                value = Factory.CreateProxy(trackable);
            }
        }

        return value;
    }
}