# Introduction

This library is built to track state changes to objects.

```
public class Person : ATrackable
{
    [Trackable]
    public virtual string? FirstName { get; set; }

    [Trackable]
    public virtual string? LastName { get; set; }
}

var person = new Person()
{
    FirstName = "John",
    LastName = "Doe"
};

var trackedPerson = Factory.CreateProxy(person);

trackedPerson!.FirstName = "Johnny";

var changes = trackedPerson.GetChangedProperties();

var (propertyName, (old, @new)) = changes.First();

Debug.Assert(propertyName.Equals(nameof(trackedPerson.FirstName)));
Debug.Assert(old!.Equals("John"));
Debug.Assert(@new!.Equals("Johnny"));
```

```
public class Address : ATrackable
{
    [Trackable]
    public virtual string? Zip { get; set; }

    [Trackable]
    public virtual string? Street { get; set; }
}

public class Person : ATrackable
{
    [Trackable]
    public virtual string? FirstName { get; set; }

    [Trackable]
    public virtual string? LastName { get; set; }

    [Trackable]
    public virtual Address Address { get; set; }
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

var trackedPerson = Factory.CreateProxy(person);

trackedPerson!.Address!.Zip = "456";

var touched = trackedPerson.GetTouchedProperties().First();

Debug.Assert(touched.Equals(nameof(trackedPerson.Address)));

var changes = trackedPerson.Address.GetChangedProperties();

var (propertyName, (old, @new)) = changes.First();

Debug.Assert(propertyName.Equals(nameof(trackedPerson.Address.Zip)));
Debug.Assert(old!.Equals("123"));
Debug.Assert(@new!.Equals("456"));
```