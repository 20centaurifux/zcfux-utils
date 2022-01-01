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
using Autofac;

namespace zcfux.DI;

public sealed class Container : IRegistry, IResolver
{
    readonly ContainerBuilder _builder = new ContainerBuilder();
    IContainer? _container;

    public void Register<T>(T instance) where T : class
    {
        FailIfBuilt();

        _builder.RegisterInstance(instance).As<T>();
    }

    public void Register<T>(Func<T> f) where T : class
    {
        FailIfBuilt();

        _builder.Register(_ => f()).As<T>();
    }

    public void Build()
    {
        FailIfBuilt();

        _container = _builder.Build();

        Built = true;
    }

    public bool Built { get; private set; }

    public T Resolve<T>() where T : class
    {
        FailIfNotBuilt();

        return _container!.Resolve<T>();
    }

    void FailIfBuilt()
    {
        if (Built)
        {
            throw new ContainerException("Container already built.");
        }
    }

    void FailIfNotBuilt()
    {
        if (!Built)
        {
            throw new ContainerException("Container has not been built yet.");
        }
    }
}