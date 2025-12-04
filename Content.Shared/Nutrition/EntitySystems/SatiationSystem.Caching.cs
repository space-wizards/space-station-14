using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.EntitySystems;

// This part deals with caching `SatiationPrototype` values by threshold.
[SuppressMessage("ReSharper", "UseCollectionExpression")] // Collection expressions use non-whitelisted functions.
public sealed partial class SatiationSystem
{
    /// <seealso cref="SatiationPrototypesCache"/>
    private readonly SatiationPrototypesCache _thresholdCache = new();

    private void InitCaching()
    {
        RepopulateThresholdCache();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<SatiationPrototype>())
            return;

        RepopulateThresholdCache();
    }

    private void RepopulateThresholdCache()
    {
        _thresholdCache.Repopulate(_prototype.EnumeratePrototypes<SatiationPrototype>());
    }

    /// <summary>
    /// Retrieves all <see cref="SatiationThresholdData"/>s for the given <paramref name="proto"/>.
    /// Note that the elements are sorted descending by <see cref="SatiationThresholdData.Threshold"/>.
    /// </summary>
    private ImmutableArray<SatiationThresholdData> GetThresholds(ProtoId<SatiationPrototype> proto)
    {
        if (_thresholdCache.GetThresholds(proto) is not { } data)
        {
            DebugTools.Assert($"Failed to get cached threshold data for satiation \"{proto}\"!");
            Log.Error($"Failed to get cached threshold data for satiation \"{proto}\"!");
            // Returning empty thresholds breaks other stuff, so return a single default threshold data.
            return ImmutableArray.Create(SatiationThresholdData.Default);
        }

        // Returning empty thresholds breaks other stuff, so return a single default threshold data.
        return data.IsEmpty ? ImmutableArray.Create(SatiationThresholdData.Default) : data;
    }

    /// <summary>
    /// Retrieves one <see cref="SatiationThresholdData"/>'s worth of fields based on <paramref name="proto"/> and
    /// <paramref name="value"/>.
    /// This should only be used for testing as it's a back door into the cache which is not meant to be accessed
    /// directly.
    /// </summary>
    [Obsolete("This function should be used exclusively for testing.")]
    public void GetThresholdDataForTesting(
        ProtoId<SatiationPrototype> proto,
        int value,
        out int threshold,
        out float decayModifier,
        out float speedModifier,
        out DamageSpecifier? damage,
        out ProtoId<AlertPrototype>? alert,
        out ProtoId<SatiationIconPrototype>? icon
    )
    {
        var data = GetCurrentAndNextLowestThresholds(
                new Satiation
                {
                    Prototype = proto,
                    LastAuthoritativeValue = value,
                    ActualDecayRate = 0,
                    LastAuthoritativeChangeTime = _timing.CurTime,
                }
            )
            .Current;

        threshold = data.Threshold;
        alert = data.Alert;
        damage = data.Damage;
        decayModifier = data.DecayModifier;
        icon = data.Icon;
        speedModifier = data.SpeedModifier;
    }

    /// <summary>
    /// Values from a <see cref="SatiationPrototype"/> which share the specified <see cref="Threshold"/>.
    /// </summary>
    private record struct SatiationThresholdData(
        int Threshold,
        float DecayModifier,
        float SpeedModifier,
        DamageSpecifier? Damage,
        ProtoId<AlertPrototype>? Alert,
        ProtoId<SatiationIconPrototype>? Icon
    )
    {
        public static readonly SatiationThresholdData Default = new(
            int.MaxValue, // Default threshold data should be the MOST top threshold always.
            1f,
            1f,
            null,
            null,
            null
        );
    }

    /// <summary>
    /// <see cref="SatiationPrototype"/> data arranged and cached for efficient lookup by threshold value.
    /// </summary>
    /// <seealso cref="SatiationThresholdData"/>
    /// <seealso cref="GetThresholds(Robust.Shared.Prototypes.ProtoId{Content.Shared.Nutrition.Prototypes.SatiationPrototype})"/>
    private sealed class SatiationPrototypesCache
    {
        private FrozenDictionary<ProtoId<SatiationPrototype>, ImmutableArray<SatiationThresholdData>> _cache =
            FrozenDictionary<ProtoId<SatiationPrototype>, ImmutableArray<SatiationThresholdData>>.Empty;

        /// <summary>
        /// Retrieves the <see cref="SatiationThresholdData"/> for the given <paramref name="proto"/>. If corresponding
        /// data for the given <paramref name="proto"/> cannot be found for some reason, returns <c>null</c>.
        /// Note that the returned array is sorted in descending order by <see cref="SatiationThresholdData.Threshold"/>.
        /// </summary>
        public ImmutableArray<SatiationThresholdData>? GetThresholds(ProtoId<SatiationPrototype> proto) =>
            _cache.TryGetValue(proto, out var thresholds) ? thresholds : null;

        /// <summary>
        /// Replaces the contents of this cache with data from <paramref name="prototypes"/>. Any
        /// <see cref="SatiationPrototype"/> in <paramref name="prototypes"/> should be accessible via
        /// <see cref="GetThresholds"/> after calling this function.
        /// </summary>
        public void Repopulate(IEnumerable<SatiationPrototype> prototypes)
        {
            _cache = prototypes.ToFrozenDictionary(ProtoId<SatiationPrototype> (proto) => proto, CalculateThresholds);
        }

        /// <summary>
        /// Constructs the data-to-cache for the given <paramref name="proto"/>.
        /// </summary>
        private static ImmutableArray<SatiationThresholdData> CalculateThresholds(SatiationPrototype proto)
        {
            // For each field in `proto` we want to cache by threshold, iterate through the by-threshold values and
            // collect them into `CachingData`s keyed by threshold value in `thresholds`.
            Dictionary<int, CachingData> thresholds = new();

            foreach (var (satiationValue, decayModifier) in proto.DecayModifiers)
            {
                AddThresholdData(satiationValue, new CachingData { DecayModifier = decayModifier });
            }

            foreach (var (satiationValue, speedModifier) in proto.SpeedModifiers)
            {
                AddThresholdData(satiationValue, new CachingData { SpeedModifier = speedModifier });
            }

            foreach (var (satiationValue, damage) in proto.Damages)
            {
                AddThresholdData(satiationValue, new CachingData { Damage = damage });
            }

            foreach (var (satiationValue, alert) in proto.Alerts)
            {
                AddThresholdData(satiationValue, new CachingData { Alert = alert });
            }

            foreach (var (satiationValue, icon) in proto.Icons)
            {
                AddThresholdData(satiationValue, new CachingData { Icon = icon });
            }

            // At this point, we should have one `CachingData` per threshold value stored in `thresholds`.

            // Finalize the data by replacing absent (see `ThresholdValue`) values with "inherited" values
            // from higher thresholds.
            var ret = ToImmutableArrayWithInheritedValues(thresholds.Values);
            DebugTools.Assert(!ret.IsEmpty, $"Calculated {proto}'s thresholds to be empty!");
            return ret;

            // This helper does X things:
            // - resolves `sv`, the threshold value, to a definite value. If it is a string key,
            //   we need to look up the value.
            // - ensures there is a `CachingData` value in `thresholds` for this threshold value.
            // - merges (see CachingData.Merge) the given `data` with the existing `CachingData` in `thresholds`.
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddThresholdData(SatiationValue sv, CachingData data)
            {
                if (proto.GetValueOrNull(sv) is not { } threshold)
                    return;

                thresholds[threshold] = (
                    thresholds.TryGetValue(threshold, out var existing)
                        ? existing
                        : new CachingData()
                ).Merge(data, proto, threshold);
            }
        }

        /// <summary>
        /// This function assembles <paramref name="data"/> into an immutable array of
        /// <see cref="SatiationThresholdData"/> for final return.
        /// Note that the returned array is ordered descending by <see cref="SatiationThresholdData.Threshold"/>
        /// </summary>
        private static ImmutableArray<SatiationThresholdData> ToImmutableArrayWithInheritedValues(
            IEnumerable<CachingData> data
        )
        {
            using var dataDescendingByThreshold = data.OrderByDescending(it => it.Threshold).GetEnumerator();
            if (!dataDescendingByThreshold.MoveNext())
                // Empty data, empty return.
                return ImmutableArray<SatiationThresholdData>.Empty;

            var ret = ImmutableArray.CreateBuilder<SatiationThresholdData>();

            var nextHigherThresholdData = dataDescendingByThreshold.Current.ToThresholdData(inheritFrom: null);
            ret.Add(nextHigherThresholdData);
            while (dataDescendingByThreshold.MoveNext())
            {
                // Create the next lower threshold data. Any absent values are replaced by their values from the next
                // higher threshold.
                var currentWithInheritedValues = dataDescendingByThreshold.Current
                    .ToThresholdData(inheritFrom: nextHigherThresholdData);
                ret.Add(currentWithInheritedValues);
                nextHigherThresholdData = currentWithInheritedValues;
            }

            return ret.ToImmutable();
        }

        /// <summary>
        /// A <a href="https://en.wikipedia.org/wiki/Option_type">Maybe</a>-like struct which contains either a value
        /// defined for a threshold, or nothing. This is needed for tracking if a threshold explicitly defines a lack of
        /// a value, or if no value is defined.
        /// </summary>
        private readonly struct ThresholdValue<T>
        {
            public static readonly ThresholdValue<T> Undefined = new();
            public static implicit operator ThresholdValue<T>(T value) => new(value);

            public T Value => IsDefined ? _value : throw new InvalidOperationException("Value is undefined");
            public T GetValueOrDefault(T value) => IsDefined ? _value : value;

            public readonly bool IsDefined = false;
            private readonly T _value;

            private ThresholdValue(T value)
            {
                _value = value;
                IsDefined = true;
            }
        }

        /// <summary>
        /// This struct is like a <see cref="SatiationThresholdData"/>, except that it keeps track of if any of its
        /// fields were undefined on the source <see cref="SatiationPrototype"/>, indicating that that field should be
        /// inherited from a higher threshold.
        /// </summary>
        private struct CachingData()
        {
            public int Threshold = int.MaxValue;

            public ThresholdValue<float> DecayModifier = ThresholdValue<float>.Undefined;
            public ThresholdValue<float> SpeedModifier = ThresholdValue<float>.Undefined;
            public ThresholdValue<DamageSpecifier?> Damage = ThresholdValue<DamageSpecifier?>.Undefined;
            public ThresholdValue<ProtoId<AlertPrototype>?> Alert = ThresholdValue<ProtoId<AlertPrototype>?>.Undefined;

            public ThresholdValue<ProtoId<SatiationIconPrototype>?> Icon =
                ThresholdValue<ProtoId<SatiationIconPrototype>?>.Undefined;

            /// <summary>
            /// Creates a new <see cref="SatiationThresholdData"/> using this record's fields, replacing any absent
            /// values with values inherited from <paramref name="inheritFrom"/>.
            /// </summary>
            public SatiationThresholdData ToThresholdData(SatiationThresholdData? inheritFrom)
            {
                var defaults = inheritFrom ?? SatiationThresholdData.Default;
                return new SatiationThresholdData(
                    Threshold,
                    DecayModifier.GetValueOrDefault(defaults.DecayModifier),
                    SpeedModifier.GetValueOrDefault(defaults.SpeedModifier),
                    Damage.GetValueOrDefault(defaults.Damage),
                    Alert.GetValueOrDefault(defaults.Alert),
                    Icon.GetValueOrDefault(defaults.Icon)
                );
            }

            /// <summary>
            /// Modifies this record by assigning any present values in <paramref name="newData"/> this record's
            /// corresponding fields. Note that if <paramref name="newData"/> defines any fields which are already
            /// defined on this record, the new value will overwire the existing one (Or throw a debug assert :^) )
            /// <paramref name="proto"/> and <paramref name="threshold"/> are only passed in to provide good assert messages.
            /// </summary>
            // !!!!!!!!!!
            // Note that failing one of these asserts is a PROTOTYPE DEFINITION ERROR and should be resolved by
            // changing the prototype definition!
            // !!!!!!!!!!
            public CachingData Merge(CachingData newData, SatiationPrototype proto, int threshold)
            {
                Threshold = threshold;

                if (newData.DecayModifier.IsDefined)
                {
                    DebugTools.Assert(!DecayModifier.IsDefined,
                        $"Error in {proto}: {nameof(SatiationThresholdData.DecayModifier)} defines conflicting values for threshold={threshold} ({newData.DecayModifier.Value} and {DecayModifier.Value})");
                    DecayModifier = newData.DecayModifier;
                }

                if (newData.SpeedModifier.IsDefined)
                {
                    DebugTools.Assert(!SpeedModifier.IsDefined,
                        $"Error in {proto}: {nameof(SatiationThresholdData.SpeedModifier)} defines conflicting values for threshold={threshold} ({newData.SpeedModifier.Value} and {SpeedModifier.Value})");
                    SpeedModifier = newData.SpeedModifier;
                }

                if (newData.Damage.IsDefined)
                {
                    DebugTools.Assert(!Damage.IsDefined,
                        $"Error in {proto}: {nameof(SatiationThresholdData.Damage)} defines conflicting values for threshold={threshold} ({newData.Damage.Value} and {Damage.Value})");
                    Damage = newData.Damage;
                }

                if (newData.Alert.IsDefined)
                {
                    DebugTools.Assert(!Alert.IsDefined,
                        $"Error in {proto}: {nameof(SatiationThresholdData.Alert)} defines conflicting values for threshold={threshold} ({newData.Alert.Value} and {Alert.Value})");
                    Alert = newData.Alert;
                }

                if (newData.Icon.IsDefined)
                {
                    DebugTools.Assert(!Icon.IsDefined,
                        $"Error in {proto}: {nameof(SatiationThresholdData.Icon)} defines conflicting values for threshold={threshold} ({newData.Icon.Value} and {Icon.Value})");
                    Icon = newData.Icon;
                }

                return this;
            }
        }
    }
}
