# Introduction

This library is built to track state changes to objects. The example below gives you a quick overview of available features.

```
using System.Diagnostics;
using zcfux.Tracking;

public class Address : ATrackable, ICloneable
{
    [Trackable]
    public virtual string? Zip { get; set; }

    [Trackable]
    public virtual string? Street { get; set; }

    public object Clone() =>
        new Address
        {
            Zip = Zip,
            Street = Street
        };
}

public class Person : ATrackable
{
    [Trackable]
    public virtual string? FirstName { get; set; }

    [Trackable]
    [Anonymize(Char = '?')]
    public virtual string? LastName { get; set; }

    [Trackable(Initial = true)]
    public virtual Address? Address { get; set; }
}

static class App
{
    static void Main(string[] args)
    {
        var person = new Person
        {
            FirstName = "John",
            LastName = "Doe",
            Address = new Address
            {
                Zip = "123",
                Street = "Main Street"
            }
        };

        // At first, we create a proxy to track changes. Class members inherited from
        // ATrackable (like Address) are converted to proxies recursively.
        var trackedPerson = Factory.CreateProxy(person);

        // Objects implementing INotifyPropertyChange are marked as "touched" when their
        // state changes. Any tracked property of a proxy fires the PropertyChanged
        // event when updated.
        trackedPerson!.Address!.Zip = "456";

        var touched = trackedPerson.GetTouchedProperties().First();

        Debug.Assert(touched == "Address");

        // Now we know that Address has been changed. Let's receive the modifications.
        var changes = trackedPerson.Address.GetChangedProperties();

        var (propertyName, (old, @new)) = changes.First();

        Debug.Assert(propertyName == "Zip");
        Debug.Assert((old as string) == "123");
        Debug.Assert((@new as string) == "456");

        // Address implements ICloneable. Therefore the initial state is still available.
        var initial = trackedPerson.GetInitialProperties();

        var initialAddress = initial["Address"] as Address;

        Debug.Assert(initialAddress!.Zip == "123");

        // You can use formatters to overwrite received initial and changed values.
        trackedPerson.LastName = "Smith";

        (_, (old, @new)) = trackedPerson.GetChangedProperties().First();

        Debug.Assert((old as string) == "???");
        Debug.Assert((@new as string) == "?????");
    }
}
```