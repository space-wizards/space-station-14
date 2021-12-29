using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos
{
    /// <summary>
    ///     A general-purpose, variable volume gas mixture.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class GasMixture : IEquatable<GasMixture>, ICloneable, ISerializationHooks
    {
        public static GasMixture SpaceGas => new() {Volume = Atmospherics.CellVolume, Temperature = Atmospherics.TCMB, Immutable = true};

        // This must always have a length that is a multiple of 4 for SIMD acceleration.
        [DataField("moles")] [ViewVariables]
        public float[] Moles = new float[Atmospherics.AdjustedNumberOfGases];

        public float[] MolesArchived = new float[Atmospherics.AdjustedNumberOfGases];

        [DataField("temperature")] [ViewVariables]
        private float _temperature = Atmospherics.TCMB;

        [DataField("immutable")] [ViewVariables]
        public bool Immutable { get; private set; }

        [DataField("lastShare")] [ViewVariables]
        public float LastShare { get; set; }

        [ViewVariables]
        public readonly Dictionary<GasReaction, float> ReactionResults = new()
        {
            // We initialize the dictionary here.
            { GasReaction.Fire, 0f }
        };

        [ViewVariables]
        public float TotalMoles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => NumericsHelpers.HorizontalAdd(Moles);
        }

        [ViewVariables]
        public float Pressure
        {
            get
            {
                if (Volume <= 0) return 0f;
                return TotalMoles * Atmospherics.R * Temperature / Volume;
            }
        }

        [ViewVariables]
        public float Temperature
        {
            get => _temperature;
            set
            {
                if (Immutable) return;
                _temperature = MathF.Max(value, Atmospherics.TCMB);
            }
        }

        public float TemperatureArchived { get; private set; }

        [DataField("volume")] [ViewVariables]
        public float Volume { get; set; }

        public GasMixture()
        {
        }

        public GasMixture(float volume = 0f)
        {
            if (volume < 0)
                volume = 0;
            Volume = volume;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkImmutable()
        {
            Immutable = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Archive()
        {
            Moles.AsSpan().CopyTo(MolesArchived.AsSpan());
            TemperatureArchived = Temperature;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMoles(int gasId)
        {
            return Moles[gasId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMoles(Gas gas)
        {
            return GetMoles((int)gas);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMoles(int gasId, float quantity)
        {
            if (!float.IsFinite(quantity) || float.IsNegative(quantity))
                throw new ArgumentException($"Invalid quantity \"{quantity}\" specified!", nameof(quantity));

            if (!Immutable)
                Moles[gasId] = quantity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetMoles(Gas gas, float quantity)
        {
            SetMoles((int)gas, quantity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdjustMoles(int gasId, float quantity)
        {
            if (!Immutable)
            {
                if (!float.IsFinite(quantity))
                    throw new ArgumentException($"Invalid quantity \"{quantity}\" specified!", nameof(quantity));

                Moles[gasId] += quantity;

                var moles = Moles[gasId];

                if (!float.IsFinite(moles) || float.IsNegative(moles))
                    throw new Exception($"Invalid mole quantity \"{moles}\" in gas Id {gasId} after adjusting moles with \"{quantity}\"!");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdjustMoles(Gas gas, float moles)
        {
            AdjustMoles((int)gas, moles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasMixture Remove(float amount)
        {
            return RemoveRatio(amount / TotalMoles);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasMixture RemoveRatio(float ratio)
        {
            switch (ratio)
            {
                case <= 0:
                    return new GasMixture(Volume){Temperature = Temperature};
                case > 1:
                    ratio = 1;
                    break;
            }

            var removed = new GasMixture(Volume) { Temperature = Temperature };

            Moles.CopyTo(removed.Moles.AsSpan());
            NumericsHelpers.Multiply(removed.Moles, ratio);
            if (!Immutable)
                NumericsHelpers.Sub(Moles, removed.Moles);

            for (var i = 0; i < Moles.Length; i++)
            {
                var moles = Moles[i];
                var otherMoles = removed.Moles[i];
                if (moles < Atmospherics.GasMinMoles || float.IsNaN(moles))
                    Moles[i] = 0;

                if (otherMoles < Atmospherics.GasMinMoles || float.IsNaN(otherMoles))
                    removed.Moles[i] = 0;
            }

            return removed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFromMutable(GasMixture sample)
        {
            if (Immutable) return;
            sample.Moles.CopyTo(Moles, 0);
            Temperature = sample.Temperature;
        }

        public enum GasCompareResult
        {
            NoExchange = -2,
            TemperatureExchange = -1,
        }

        /// <summary>
        ///     Compares sample to self to see if within acceptable ranges that group processing may be enabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GasCompareResult Compare(GasMixture sample)
        {
            var moles = 0f;

            for(var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var gasMoles = Moles[i];
                var delta = MathF.Abs(gasMoles - sample.Moles[i]);
                if (delta > Atmospherics.MinimumMolesDeltaToMove && (delta > gasMoles * Atmospherics.MinimumAirRatioToMove))
                    return (GasCompareResult)i; // We can move gases!
                moles += gasMoles;
            }

            if (moles > Atmospherics.MinimumMolesDeltaToMove)
            {
                var tempDelta = MathF.Abs(Temperature - sample.Temperature);
                if (tempDelta > Atmospherics.MinimumTemperatureDeltaToSuspend)
                    return GasCompareResult.TemperatureExchange; // There can be temperature exchange.
            }

            // No exchange at all!
            return GasCompareResult.NoExchange;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (Immutable) return;
            Array.Clear(Moles, 0, Atmospherics.TotalNumberOfGases);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(float multiplier)
        {
            if (Immutable) return;
            NumericsHelpers.Multiply(Moles, multiplier);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // The arrays MUST have a specific length.
            Array.Resize(ref Moles, Atmospherics.AdjustedNumberOfGases);
            Array.Resize(ref MolesArchived, Atmospherics.AdjustedNumberOfGases);
        }

        public override bool Equals(object? obj)
        {
            if (obj is GasMixture mix)
                return Equals(mix);
            return false;
        }

        public bool Equals(GasMixture? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Moles.SequenceEqual(other.Moles)
                   && MolesArchived.SequenceEqual(other.MolesArchived)
                   && _temperature.Equals(other._temperature)
                   && ReactionResults.SequenceEqual(other.ReactionResults)
                   && Immutable == other.Immutable
                   && LastShare.Equals(other.LastShare)
                   && TemperatureArchived.Equals(other.TemperatureArchived)
                   && Volume.Equals(other.Volume);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
            {
                var moles = Moles[i];
                var molesArchived = MolesArchived[i];
                hashCode.Add(moles);
                hashCode.Add(molesArchived);
            }

            hashCode.Add(_temperature);
            hashCode.Add(TemperatureArchived);
            hashCode.Add(Immutable);
            hashCode.Add(LastShare);
            hashCode.Add(Volume);

            return hashCode.ToHashCode();
        }

        public object Clone()
        {
            var newMixture = new GasMixture()
            {
                Moles = (float[])Moles.Clone(),
                MolesArchived = (float[])MolesArchived.Clone(),
                _temperature = _temperature,
                Immutable = Immutable,
                LastShare = LastShare,
                TemperatureArchived = TemperatureArchived,
                Volume = Volume,
            };
            return newMixture;
        }
    }
}
