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

namespace zcfux.Data.Proxy;

public static class Factory
{
    static readonly ProxyGenerator Generator = new();

    public static TInterface ConvertHandle<TInterface, TImpl>()
        where TInterface : class
    {
        var interceptor = new ConvertHandleInterceptor<TInterface, TImpl>();

        var proxy = Generator.CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);

        return proxy!;
    }

    public static TInterface PrependHandle<TInterface, TImpl>(object handle)
        where TInterface : class
        where TImpl : class
    {
        var impl = Activator.CreateInstance<TImpl>();

        return PrependHandle<TInterface, TImpl>(impl, handle);
    }

    public static TInterface PrependHandle<TInterface, TImpl>(TImpl impl, object handle)
        where TInterface : class
        where TImpl : class
    {
        var interceptor = new PrependHandleInterceptor<TImpl>(impl, handle);

        var proxy = Generator.CreateInterfaceProxyWithoutTarget<TInterface>(interceptor);

        return proxy!;
    }
}