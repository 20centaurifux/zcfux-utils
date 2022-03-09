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
using zcfux.Replication.Generic;

namespace zcfux.Replication;

public sealed class MergeAlgorithms
{
    readonly Dictionary<Type, object> _m = new();
    bool _built = false;

    public void Register<T>(IMergeAlgorithm<T> merge)
        where T : IEntity
    {
        ThrowIfBuilt();

        _m[typeof(T)] = merge;
    }

    public void Register<T>(IMergeAlgorithm mergeAlgorithm)
        where T : IEntity
    {
        ThrowIfBuilt();

        _m[typeof(T)] = mergeAlgorithm;
    }

    public void Build()
    {
        ThrowIfBuilt();

        _built = true;
    }

    void ThrowIfBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("Registry has been built.");
        }
    }

    public IMergeAlgorithm<T> GetNonGeneric<T>()
        where T : IEntity
    {
        ThrowIfNotBuilt();

        return (_m[typeof(T)] is IMergeAlgorithm<T> algorithm)
            ? algorithm
            : throw new ArgumentException($"Algorithm for type `{typeof(T)}' not found.");
    }

    public IMergeAlgorithm GetGeneric<T>()
        where T : IEntity
        => GetGeneric(typeof(T));

    public IMergeAlgorithm GetGeneric(Type type)
    {
        ThrowIfNotBuilt();

        return (_m[type] is IMergeAlgorithm algorithm)
            ? algorithm
            : throw new ArgumentException($"Algorithm for type `{type}' not found.");
    }

    public IVersion Merge(IVersion version, IVersion[] conflicts)
    {
        IVersion? mergedVersion = null;

        var entityType = version.Entity.GetType();

        var algorithm = _m[entityType];

        var algorithmType = algorithm.GetType();

        if (algorithmType
            .GetInterfaces()
            .Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IMergeAlgorithm<>)))
        {
            var versionType = typeof(Version<>).MakeGenericType(entityType);

            var ctor = versionType.GetConstructor(new[] { typeof(IVersion) })!;

            dynamic typedVersion = ctor.Invoke(new object?[] { version });

            dynamic typedConflicts = Activator.CreateInstance(
                versionType.MakeArrayType(),
                new object[] { conflicts.Length })!;

            conflicts
                .Select(c => ctor.Invoke(new object?[] { c }))
                .ToArray()
                .CopyTo(typedConflicts, 0);

            typedVersion = algorithmType
                .GetMethod("Merge")!
                .Invoke(algorithm, new[] { typedVersion, typedConflicts })!;

            mergedVersion = new Version<IEntity>(
                typedVersion.Entity,
                typedVersion.Revision,
                typedVersion.Side,
                typedVersion.Modified,
                typedVersion.IsDeleted);
        }
        else if (algorithm is IMergeAlgorithm genericAlgorithm)
        {
            mergedVersion = genericAlgorithm.Merge(version, conflicts);
        }

        return mergedVersion
               ?? throw new ArgumentException($"Algorithm for type `{entityType}' not found.");
    }

    void ThrowIfNotBuilt()
    {
        if (!_built)
        {
            throw new InvalidOperationException("Registry has not been built.");
        }
    }
}