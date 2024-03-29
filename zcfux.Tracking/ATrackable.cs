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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Castle.DynamicProxy;

namespace zcfux.Tracking;

public abstract class ATrackable
    : IInitialProperties
    , IChangedProperties
    , ITouchedProperties
    , INotifyPropertyChanged
    , ITrackable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    readonly Lazy<(PropertyInfo, TrackableAttribute)[]> _properties;
    IDictionary<string, IFormatter>? _formatters;

    protected ATrackable()
        => _properties = new Lazy<(PropertyInfo, TrackableAttribute)[]>(() => GetProperties().ToArray());

    public void AssignFrom(ATrackable source)
    {
        foreach (var (prop, _) in source._properties.Value)
        {
            var value = prop.GetValue(source);

            prop.SetValue(this, value);
        }
    }

    #region IInitialProperties

    readonly IDictionary<string, object?> _initialProperties = new Dictionary<string, object?>();

    void IInitialProperties.InitializeProperty(string propertyName, object? value)
        => _initialProperties[propertyName] = value?.Copy();

    public IReadOnlyDictionary<string, object?> GetInitialProperties()
        => ProxyUtil.IsProxy(this)
            ? new ReadOnlyDictionary<string, object?>(_initialProperties)
            : throw new InvalidOperationException();

    #endregion

    #region IChangedProperties

    readonly IDictionary<string, ChangedValue> _changedProperties = new Dictionary<string, ChangedValue>();

    void IChangedProperties.ChangeProperty(string propertyName, object? value)
    {
        var prop = this.GetType()
            .GetProperties()
            .Single(p => p.Name == propertyName);

        if (prop.PropertyType.IsAssignableTo(typeof(INotifyPropertyChanged)))
        {
            NotifierChanged(prop, value as INotifyPropertyChanged);
        }
        else
        {
            ValueChanged(prop, value);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    void NotifierChanged(PropertyInfo prop, INotifyPropertyChanged? trackable)
    {
        var oldTrackable = prop.GetValue(this) as INotifyPropertyChanged;

        if (!ReferenceEquals(oldTrackable, trackable))
        {
            if (trackable != null)
            {
                trackable.PropertyChanged += NewPropertyChangedHandler(prop.Name);
            }

            if (!Equals(oldTrackable, trackable))
            {
                _touchedProperties.Add(prop.Name);
            }
        }
    }

    void ValueChanged(PropertyInfo prop, object? value)
    {
        if (_changedProperties.TryGetValue(prop.Name, out var entry))
        {
            if (Equals(entry.Old, value))
            {
                _changedProperties.Remove(prop.Name);
            }
            else
            {
                _changedProperties[prop.Name] = new ChangedValue(entry.Old, value?.Copy());
            }
        }
        else
        {
            var oldValue = prop.GetValue(this);

            if (!Equals(oldValue, value))
            {
                _changedProperties[prop.Name] = new ChangedValue(oldValue?.Copy(), value?.Copy());
            }
        }
    }

    public IReadOnlyDictionary<string, ChangedValue> GetChangedProperties()
        => ProxyUtil.IsProxy(this)
            ? FormatChangedProperties()
            : throw new InvalidOperationException();

    ReadOnlyDictionary<string, ChangedValue> FormatChangedProperties()
    {
        var m = new Dictionary<string, ChangedValue>();

        foreach (var (propertyName, (old, @new)) in _changedProperties)
        {
            if (_formatters!.TryGetValue(propertyName, out var formatter))
            {
                m[propertyName] = new ChangedValue(formatter.Format(old), formatter.Format(@new));
            }
            else
            {
                m[propertyName] = new ChangedValue(old, @new);
            }
        }

        return new ReadOnlyDictionary<string, ChangedValue>(m);
    }

    #endregion

    #region ITouchedProperties

    readonly ISet<string> _touchedProperties = new HashSet<string>();

    void ITouchedProperties.TouchProperty(string propertyName)
        => _touchedProperties.Add(propertyName);

    public IEnumerable<string> GetTouchedProperties()
        => ProxyUtil.IsProxy(this)
            ? _touchedProperties
            : throw new InvalidOperationException();

    #endregion

    #region internal

    internal ATrackable ShallowCopy()
        => (MemberwiseClone() as ATrackable)!;

    internal void WrapNotifiers()
    {
        foreach (var (prop, _) in _properties.Value)
        {
            if (prop.GetValue(this) is ATrackable trackable
                && !ProxyUtil.IsProxy(trackable))
            {
                var proxy = Factory.CreateProxy(trackable);

                prop.SetValue(this, proxy);
            }
        }
    }

    internal void CloneMembers()
    {
        foreach (var (prop, _) in _properties.Value)
        {
            var clone = prop.GetValue(this)?.Copy();

            prop.SetValue(this, clone);
        }
    }

    internal static void CreateFormatters(ATrackable origin, ATrackable proxy)
    {
        proxy._formatters = new Dictionary<string, IFormatter>();

        foreach (var prop in origin.GetType().GetProperties())
        {
            var attr = prop.GetCustomAttributes(typeof(FormatterAttribute), true)
                .Cast<FormatterAttribute>()
                .SingleOrDefault();

            if (attr != null)
            {
                proxy._formatters[prop.Name] = Formatters.Factory.CreateFormatter(attr);
            }
        }
    }

    internal void CopyInitials(ATrackable proxy)
    {
        foreach (var (prop, attr) in _properties.Value)
        {
            if (attr.Initial)
            {
                var value = prop.GetValue(this)?.Copy();

                if (proxy._formatters!.TryGetValue(prop.Name, out var formatter))
                {
                    proxy._initialProperties[prop.Name] = formatter.Format(value);
                }
                else
                {
                    proxy._initialProperties[prop.Name] = value?.Copy();
                }
            }
        }
    }

    internal static void WatchNotifiers(ATrackable origin, ATrackable proxy)
    {
        foreach (var (prop, _) in origin._properties.Value)
        {
            if (prop.GetValue(proxy) is INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged += proxy.NewPropertyChangedHandler(prop.Name);
            }
        }
    }

    IEnumerable<(PropertyInfo, TrackableAttribute)> GetProperties()
    {
        foreach (var prop in GetType().GetProperties())
        {
            var attr = prop.GetCustomAttributes(typeof(TrackableAttribute), true)
                .Cast<TrackableAttribute>()
                .SingleOrDefault();

            if (attr != null)
            {
                yield return (prop, attr);
            }
        }
    }

    PropertyChangedEventHandler NewPropertyChangedHandler(string propertyName) => (s, e) =>
    {
        _touchedProperties.Add(propertyName);

        PropertyChanged?.Invoke(s, new PropertyChangedEventArgs(propertyName));
    };

    #endregion
}