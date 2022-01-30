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
using System.ComponentModel;

namespace zcfux.Tracking.Test;

public class A : ATrackable
{
    [Trackable]
    public virtual int Number { get; set; }
}

public class B : ATrackable
{
    [Trackable(Initial = true)]
    public virtual int Number { get; set; }
}

public sealed class Record : ICloneable
{
    readonly int _hashCode;

    public Record(string value)
        => (_hashCode, Value) = (value.GetHashCode(), value);

    public string Value { get; set; }

    public override bool Equals(object? obj)
        => (obj is Record record) && record.Value.Equals(Value);

    public override int GetHashCode()
        => _hashCode;

    public object Clone()
        => new Record(Value);
}

public class C : ATrackable
{
    [Trackable]
    public virtual Record? Reference { get; set; }
}

public class D : ATrackable
{
    [Trackable(Initial = true)]
    public virtual Record? Reference { get; set; }
}

public class Notifier : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    string? _value;

    public string? Value
    {
        get => _value;

        set
        {
            _value = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}

public class E : ATrackable
{
    [Trackable]
    public virtual Notifier? Notifier { get; set; }
}

public class F : ATrackable
{
    [Trackable]
    public virtual A? Trackable { get; set; }
}

public class G : ATrackable
{
    [Trackable]
    public virtual int A { get; set; }

    [Trackable]
    public virtual int B { get; set; }

    [Trackable]
    public virtual int C { get; set; }
}

public class H : ATrackable
{
    [Trackable(Initial = true)]
    [Formatter("zcfux.Tracking.Test", "zcfux.Tracking.Test.ReverseFormatter")]
    public virtual string? Value { get; set; }
}

public class I : ATrackable
{
    [Trackable(Initial = true)]
    [Anonymize(Char = '#')]
    public virtual string? Value { get; set; }
}