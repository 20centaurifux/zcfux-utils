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

namespace zcfux.Tracking.Test;

public sealed class Tests
{
    [Test]
    public void GetTrackedPropertiesFromModel()
    {
        var model = new A();

        Assert.Throws<InvalidOperationException>(() => model.GetInitialProperties());
        Assert.Throws<InvalidOperationException>(() => model.GetChangedProperties());
        Assert.Throws<InvalidOperationException>(() => model.GetTouchedProperties());
    }

    [Test]
    public void ChangedValueIsTracked()
    {
        var model = new A
        {
            Number = 23
        };

        var proxy = Factory.CreateProxy(model);

        proxy!.Number = 42;

        Assert.IsEmpty(proxy.GetInitialProperties());
        Assert.IsEmpty(proxy.GetTouchedProperties());

        var changed = proxy.GetChangedProperties().ToArray();

        Assert.AreEqual(1, changed.Length);

        var (key, (old, @new)) = changed.First();

        Assert.AreEqual(nameof(proxy.Number), key);
        Assert.AreEqual(23, old);
        Assert.AreEqual(42, @new);
    }

    [Test]
    public void RevertedValueIsNotTracked()
    {
        var model = new A
        {
            Number = 23
        };

        var proxy = Factory.CreateProxy(model);

        proxy!.Number = 42;
        proxy.Number = 23;

        Assert.IsEmpty(proxy.GetInitialProperties());
        Assert.IsEmpty(proxy.GetTouchedProperties());
        Assert.IsEmpty(proxy.GetChangedProperties());
    }

    [Test]
    public void InitialAndChangedValueTracked()
    {
        var model = new B
        {
            Number = 23
        };

        var proxy = Factory.CreateProxy(model);

        proxy!.Number = 42;

        var initial = proxy.GetInitialProperties().ToArray();

        Assert.AreEqual(1, initial.Length);

        var (key, value) = initial.First();

        Assert.AreEqual(nameof(proxy.Number), key);
        Assert.AreEqual(23, value);

        Assert.IsEmpty(proxy.GetTouchedProperties());

        var changed = proxy.GetChangedProperties().ToArray();

        Assert.AreEqual(1, changed.Length);

        (key, var (old, @new)) = changed.First();

        Assert.AreEqual(nameof(proxy.Number), key);
        Assert.AreEqual(23, old);
        Assert.AreEqual(42, @new);
    }

    [Test]
    public void ChangedReferenceIsTracked()
    {
        var model = new C
        {
            Reference = new Record("hello")
        };

        var proxy = Factory.CreateProxy(model);

        proxy!.Reference = new Record("hello");

        Assert.IsEmpty(proxy.GetInitialProperties());
        Assert.IsEmpty(proxy.GetTouchedProperties());
        Assert.IsEmpty(proxy.GetChangedProperties());

        proxy.Reference = new Record("world");

        var changed = proxy.GetChangedProperties().ToArray();

        Assert.AreEqual(1, changed.Length);

        var (key, (old, @new)) = changed.First();

        Assert.AreEqual(nameof(proxy.Reference), key);
        Assert.AreEqual("hello", (old as Record)!.Value);
        Assert.AreEqual("world", (@new as Record)!.Value);
    }

    [Test]
    public void MutatedReferenceIsNotTracked()
    {
        var model = new C
        {
            Reference = new Record("hello")
        };

        var proxy = Factory.CreateProxy(model);

        proxy!.Reference!.Value = "world";

        Assert.IsEmpty(proxy.GetInitialProperties());
        Assert.IsEmpty(proxy.GetTouchedProperties());
        Assert.IsEmpty(proxy.GetChangedProperties());
    }

    [Test]
    public void ReferenceIsCloned()
    {
        var model = new D
        {
            Reference = new Record("hello")
        };

        var proxy = Factory.CreateProxy(model);

        Assert.AreNotSame(model.Reference, proxy!.Reference);
    }

    [Test]
    public void ChangedValueIsCloned()
    {
        var model = new D
        {
            Reference = new Record("hello")
        };

        var proxy = Factory.CreateProxy(model);

        proxy!.Reference = new Record("world");

        var (_, (old, @new)) = proxy.GetChangedProperties().First();

        Assert.AreNotSame(model.Reference, old);
        Assert.AreEqual(model.Reference, old);

        Assert.AreNotSame(proxy.Reference, old);
        Assert.AreNotEqual(proxy.Reference, old);

        Assert.AreNotSame(proxy.Reference, @new);
        Assert.AreEqual(proxy.Reference, @new);
    }

    [Test]
    public void InitialValueIsCloned()
    {
        var model = new D
        {
            Reference = new Record("hello world")
        };

        var proxy = Factory.CreateProxy(model);

        var (_, reference) = proxy!.GetInitialProperties().First();

        Assert.AreNotSame(model.Reference, reference);
        Assert.AreEqual(model.Reference, reference);

        Assert.AreNotSame(proxy.Reference, reference);
        Assert.AreEqual(proxy.Reference, reference);
    }

    [Test]
    public void TouchedValueIsTracked()
    {
        var model = new E
        {
            Notifier = new Notifier()
        };

        var proxy = Factory.CreateProxy(model);

        Assert.IsEmpty(proxy!.GetInitialProperties());
        Assert.IsEmpty(proxy.GetTouchedProperties());
        Assert.IsEmpty(proxy.GetChangedProperties());

        proxy.Notifier!.Value = "hello world";

        Assert.IsEmpty(proxy.GetInitialProperties());
        Assert.IsEmpty(proxy.GetChangedProperties());

        var touched = proxy.GetTouchedProperties().First();

        Assert.AreEqual(nameof(proxy.Notifier), touched);
    }

    [Test]
    public void ChangedValueOfNestedTrackableIsTracked()
    {
        var model = new F
        {
            Trackable = new A
            {
                Number = 23
            }
        };

        var proxy = Factory.CreateProxy(model);

        Assert.IsEmpty(proxy!.GetInitialProperties());
        Assert.IsEmpty(proxy.GetTouchedProperties());
        Assert.IsEmpty(proxy.GetChangedProperties());

        proxy.Trackable!.Number = 42;

        Assert.IsEmpty(proxy.GetInitialProperties());
        Assert.IsEmpty(proxy.GetChangedProperties());

        var touched = proxy.GetTouchedProperties().First();

        Assert.AreEqual(nameof(proxy.Trackable), touched);

        var (key, (old, @new)) = proxy.Trackable.GetChangedProperties().First();

        Assert.AreEqual(nameof(proxy.Trackable.Number), key);
        Assert.AreEqual(23, old);
        Assert.AreEqual(42, @new);
    }

    [Test]
    public void AssignFrom()
    {
        var before = new G
        {
            A = 1,
            B = 2,
            C = 3
        };

        var proxy = Factory.CreateProxy(before);

        var after = new G
        {
            A = 1,
            B = 23,
            C = 42
        };

        proxy!.AssignFrom(after);

        Assert.IsEmpty(proxy.GetInitialProperties());
        Assert.IsEmpty(proxy.GetTouchedProperties());

        var changes = proxy.GetChangedProperties().ToArray();

        Assert.AreEqual(2, changes.Length);

        var diff = new Dictionary<string, ChangedValue>(changes);

        var (old, @new) = diff[nameof(proxy.B)];

        Assert.AreEqual(2, old);
        Assert.AreEqual(23, @new);

        (old, @new) = diff[nameof(proxy.C)];

        Assert.AreEqual(3, old);
        Assert.AreEqual(42, @new);
    }

    [Test]
    public void Formatter()
    {
        var model = new H
        {
            Value = "redrum"
        };

        var proxy = Factory.CreateProxy(model);

        proxy!.Value = "murder";

        var (_, value) = proxy.GetInitialProperties().First();

        Assert.AreEqual("murder", value);

        var (_, (old, @new)) = proxy.GetChangedProperties().First();

        Assert.AreEqual("murder", old);
        Assert.AreEqual("redrum", @new);
    }

     [Test]
     public void Anonymize()
     {
         var model = new I
         {
             Value = "hello world"
         };

         var proxy = Factory.CreateProxy(model);

         var (_, value) = proxy!.GetInitialProperties().First();

         Assert.AreEqual("###########", value);
     }
}