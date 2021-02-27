using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CollectionChanges
{
    [Flags]
    public enum ChangeOptions
    {
        AddedSources = 1 << 1,
        DeletedCurrents = 1 << 2,
        Intersect = 1 << 3,
        All = AddedSources | DeletedCurrents | Intersect
    }

    public class Changes<TCurrent, TSource>
    {
        public IReadOnlyList<TSource> AddedSources { get; set; }
        public IReadOnlyList<TCurrent> DeletedCurrents { get; set; }
        public IReadOnlyList<(TCurrent Current, TSource Source)> Intersect { get; set; }
    }

    public static class CollectionChanges
    {
        private class FuncEqualityComparer<T> : EqualityComparer<T>
        {
            private readonly Func<T, T, bool> _comparer;

            public FuncEqualityComparer(Func<T, T, bool> comparer)
            {
                _comparer = comparer;
            }

            public override bool Equals(T x, T y)
            {
                return _comparer(x, y);
            }

            public override int GetHashCode(T obj)
            {
                return obj.GetHashCode();
            }
        }

        private static FuncEqualityComparer<(TCurrent, TSource)> GetEqualityComparer<TCurrent, TSource>(Func<TCurrent, TSource, bool> equalityComparer)
        {
            return new FuncEqualityComparer<(TCurrent Current, TSource Source)>((x, y) =>
            {
                // Compare two pairs of value. We rely on the fact that inside the pair the values are equal.
                Debug.Assert(equalityComparer(y.Current, x.Source));
                return equalityComparer(x.Current, y.Source);
            });
        }

        public static Changes<TCurrent, TSource> GetChanges<TCurrent, TSource>(
            IEnumerable<TCurrent> currents,
            IEnumerable<TSource> sources,
            Func<TCurrent, TSource, bool> equalityComparer,
            ChangeOptions options = ChangeOptions.All)
        {
            if (currents == null)
                throw new ArgumentNullException(nameof(currents));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            if (equalityComparer == null)
                throw new ArgumentNullException(nameof(equalityComparer));

            var intersect = options.HasFlag(ChangeOptions.Intersect)
                ? new HashSet<(TCurrent Current, TSource Source)>(GetEqualityComparer(equalityComparer))
                : null;
            var deletedCurrents = options.HasFlag(ChangeOptions.DeletedCurrents) ? new List<TCurrent>() : null;
            var addedSources = options.HasFlag(ChangeOptions.AddedSources) ? new List<TSource>() : null;

            foreach (var source in sources)
            {
                if (TryGetFirst(currents, c => equalityComparer(c, source), out var current))
                    intersect?.Add((current, source));
                else
                    addedSources?.Add(source);
            }

            foreach (var current in currents)
            {
                if (TryGetFirst(sources, s => equalityComparer(current, s), out var source))
                    intersect?.Add((current, source));
                else
                    deletedCurrents?.Add(current);
            }

            return new Changes<TCurrent, TSource>
            {
                AddedSources = addedSources,
                DeletedCurrents = deletedCurrents,
                Intersect = intersect?.ToArray()
            };
        }

        public static Changes<TCurrent, TSource> GetChanges<TCurrent, TSource, TKey>(
            IReadOnlyDictionary<TKey, TCurrent> currents,
            IReadOnlyDictionary<TKey, TSource> sources,
            ChangeOptions options = ChangeOptions.All)
        {
            if (currents == null)
                throw new ArgumentNullException(nameof(currents));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            var intersect = options.HasFlag(ChangeOptions.Intersect) ? new List<(TCurrent Current, TSource Source)>() : null;
            var deletedCurrents = options.HasFlag(ChangeOptions.DeletedCurrents) ? new List<TCurrent>() : null;
            var addedSources = options.HasFlag(ChangeOptions.AddedSources) ? new List<TSource>() : null;

            foreach (var source in sources)
            {
                if (currents.TryGetValue(source.Key, out var current))
                    intersect?.Add((current, source.Value));
                else
                    addedSources?.Add(source.Value);
            }

            foreach (var current in currents)
            {
                if (sources.TryGetValue(current.Key, out var source))
                    intersect?.Add((current.Value, source));
                else
                    deletedCurrents?.Add(current.Value);
            }

            return new Changes<TCurrent, TSource>
            {
                AddedSources = addedSources,
                DeletedCurrents = deletedCurrents,
                Intersect = intersect
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetFirst<T>(
            IEnumerable<T> items,
            Func<T, bool> predicate,
            out T found)
        {
            foreach (var i in items)
            {
                if (predicate(i))
                {
                    found = i;
                    return true;
                }
            }

            found = default(T);
            return false;
        }
    }
}
