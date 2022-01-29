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
    public virtual string? LastName { get; set; }

    [Trackable(Initial = true)]
    public virtual Address? Address { get; set; }
}

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

// Create a proxy to track changes.
var trackedPerson = Factory.CreateProxy(person);

// Objects implementing INotifyPropertyChanged are marked as "touched" when
// they're changed. Nested properties inherited from ATrackable (like
// "Address") are converted to proxies automatically.
trackedPerson!.Address!.Zip = "456"; // fires PropertyChanged event

var touched = trackedPerson.GetTouchedProperties().First();

Debug.Assert(touched == "Address");

// Receive changed properties from the "Address" proxy.
var changes = trackedPerson.Address.GetChangedProperties();

var (propertyName, (old, @new)) = changes.First();

Debug.Assert(propertyName == "Zip");
Debug.Assert((old as string) == "123");
Debug.Assert((@new as string) == "456");

// Properties implementing ICloneable are copied whenever necessary,
// hence the state of the initial address instance is still available.
var initial = trackedPerson.GetInitialProperties();

var initialAddress = initial["Address"] as Address;

Debug.Assert(initialAddress!.Zip == "123");
```