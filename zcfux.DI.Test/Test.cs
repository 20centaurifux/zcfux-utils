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
using NUnit.Framework;

namespace zcfux.DI.Test;

public sealed class Test
{
    [Test]
    public void BuildContainer()
    {
        var container = new Container();

        Assert.IsFalse(container.Built);

        container.Build();

        Assert.IsTrue(container.Built);
    }

    [Test]
    public void BuildTwice()
    {
        var container = new Container();

        container.Build();

        Assert.Throws<ContainerException>(() => container.Build());
    }

    [Test]
    public void ResolveBeforeBuild()
    {
        var container = new Container();

        Assert.Throws<ContainerException>(() => container.Resolve<string>());
    }

    [Test]
    public void RegisterAfterBuild()
    {
        var container = new Container();

        container.Build();

        Assert.Throws<ContainerException>(() => container.Register(string.Empty));
    }

    [Test]
    public void RegisterAndResolveClass()
    {
        var container = new Container();

        var a = TestContext.CurrentContext.Random.GetString();

        container.Register(a);

        container.Build();

        var b = container.Resolve<string>();

        Assert.AreSame(a, b);
    }

    [Test]
    public void RegisterAndResolveFunction()
    {
        var container = new Container();

        container.Register(() => TestContext.CurrentContext.Random.GetString());

        container.Build();

        var a = container.Resolve<string>();
        var b = container.Resolve<string>();

        Assert.AreNotSame(a, b);
        Assert.AreNotEqual(a, b);
    }

    [Test]
    public void ResolveUnknown()
    {
        var container = new Container();

        container.Build();

        Assert.That(() => container.Resolve<string>(), Throws.Exception);
    }
}