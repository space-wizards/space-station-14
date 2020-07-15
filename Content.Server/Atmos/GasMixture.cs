using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Math = CannyFastMath.Math;
using MathF = CannyFastMath.MathF;

namespace Content.Server.Atmos
{
    /// <summary>
    /// A general-purposes, variable volume gas mixture.
    /// </summary>
    public class GasMixture
    {
        private static ReagentPrototype GetReagent(string gasId) =>
            IoCManager.Resolve<IPrototypeManager>().Index<ReagentPrototype>(gasId);

        private static Dictionary<string, ValueTuple<float, float>> GetCombinedContents(GasMixture a, GasMixture b)
        {
            var dictionary = new Dictionary<string, ValueTuple<float, float>>();

            foreach (var (gas, value) in a._contents)
            {
                dictionary[gas] = (value, 0f);
            }

            foreach (var (gas, value) in b._contents)
            {
                if (dictionary.ContainsKey(gas))
                    dictionary[gas] = (dictionary[gas].Item1, value);
                else
                    dictionary[gas] = (0f, value);
            }

            return dictionary;
        }

        private Dictionary<string, float> _contents = new Dictionary<string, float>();
        private Dictionary<string, float> _contentsArchived = new Dictionary<string, float>();

        public bool Immutable { get; private set; }
        public float LastShare { get; private set; } = 0;

        public float HeatCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var capacity = 0f;

                foreach (var gas in _contents)
                {
                    capacity += GetReagent(gas.Key).SpecificHeat * gas.Value;
                }

                return MathF.Min(capacity, MinimumHeatCapacity);
            }
        }

        public float HeatCapacityArchived
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var capacity = 0f;

                foreach (var gas in _contentsArchived)
                {
                    capacity += GetReagent(gas.Key).SpecificHeat * gas.Value;
                }

                return MathF.Min(capacity, MinimumHeatCapacity);
            }
        }

        public float Moles
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var capacity = 0f;

                foreach (var gas in _contents)
                {
                    capacity += gas.Value;
                }

                return capacity;
            }
        }

        public float Pressure
        {
            get
            {
                if (Volume <= 0) return 0f;
                return Moles * Atmospherics.R * Temperature / Volume;
            }
        }

        public float ThermalEnergy => Temperature * HeatCapacity;

        public float Temperature { get; set; }

        public float TemperatureArchived { get; set; }

        public virtual float Volume { get; protected set; }

        public float MinimumHeatCapacity { get; set; }

        public GasMixture()
        {
        }

        public GasMixture(float volume)
        {
            if (volume < 0)
                volume = 0;
            Volume = volume;
        }

        public void MarkImmutable()
        {
            Immutable = true;
        }

        public void Archive()
        {
            _contentsArchived = new Dictionary<string, float>(_contents);
            TemperatureArchived = Temperature;
        }

        public void Merge(GasMixture giver)
        {
            if (Immutable) return;

            if (MathF.Abs(Temperature - giver.Temperature) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var combinedHeatCapacity = HeatCapacity + giver.HeatCapacity;
                if (combinedHeatCapacity > 0f)
                {
                    Temperature =
                        (giver.Temperature * giver.HeatCapacity + Temperature * HeatCapacity) / combinedHeatCapacity;
                }
            }

            foreach (var (gas, moles) in giver._contents)
            {
                if (_contents.ContainsKey(gas))
                    _contents[gas] += moles;
                else
                    _contents[gas] = moles;
            }
        }

        public void Add(string gasId, float quantity)
        {
            if (_contents.ContainsKey(gasId))
                _contents[gasId] += quantity;
            else
                _contents[gasId] = quantity;
        }

        public GasMixture Remove(float amount)
        {
            return RemoveRatio(amount / Moles);
        }

        public GasMixture RemoveRatio(float ratio)
        {
            if(ratio <= 0)
                return new GasMixture(Volume);

            if (ratio > 1)
                ratio = 1;

            var removed = new GasMixture();
            removed.Volume = Volume;
            removed.Temperature = Temperature;

            foreach (var (gas, moles) in _contents)
            {
                if (moles < Atmospherics.GasMinMoles)
                    removed._contents[gas] = 0f;
                else
                {
                    var removedMoles = moles * ratio;
                    removed._contents[gas] = removedMoles;
                    if(!Immutable)
                        _contents[gas] -= removedMoles;
                }
            }

            return removed;
        }

        public void CopyFromMutable(GasMixture sample)
        {
            if (Immutable) return;
            _contents = new Dictionary<string, float>(sample._contents);
            Temperature = sample.Temperature;
        }

        public float Share(GasMixture sharer, int atmosAdjacentTurfs)
        {
            var temperatureDelta = TemperatureArchived - sharer.TemperatureArchived;
            var absTemperatureDelta = Math.Abs(temperatureDelta);
            var oldHeatCapacity = 0f;
            var oldSharerHeatCapacity = 0f;

            if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                oldHeatCapacity = HeatCapacity;
                oldSharerHeatCapacity = sharer.HeatCapacity;
            }

            var heatCapacityToSharer = 0f;
            var heatCapacitySharerToThis = 0f;
            var movedMoles = 0f;
            var absMovedMoles = 0f;

            foreach (var (gas, (thisValue, sharerValue)) in GetCombinedContents(this, sharer))
            {
                var delta = (thisValue - sharerValue) / (atmosAdjacentTurfs + 1);
                if (MathF.Abs(delta) >= Atmospherics.GasMinMoles)
                {
                    if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
                    {
                        var gasHeatCapacity = delta * GetReagent(gas).SpecificHeat;
                        if (delta > 0)
                        {
                            heatCapacityToSharer += gasHeatCapacity;
                        }
                        else
                        {
                            heatCapacitySharerToThis -= gasHeatCapacity;
                        }
                    }

                    if (!Immutable) _contents[gas] -= delta;
                    if (!sharer.Immutable) sharer._contents[gas] += delta;
                    movedMoles += delta;
                    absMovedMoles += MathF.Abs(delta);
                }
            }

            LastShare = absMovedMoles;

            if (absTemperatureDelta > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var newHeatCapacity = oldHeatCapacity + heatCapacitySharerToThis - heatCapacityToSharer;
                var newSharerHeatCapacity = oldSharerHeatCapacity + heatCapacityToSharer - heatCapacitySharerToThis;

                // Transfer of thermal energy (via changed heat capacity) between self and sharer.
                if (!Immutable && newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    Temperature = (oldHeatCapacity * Temperature - heatCapacityToSharer * TemperatureArchived + heatCapacitySharerToThis * sharer.TemperatureArchived) / newHeatCapacity;
                }

                if (!sharer.Immutable && newSharerHeatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    sharer.Temperature = (oldSharerHeatCapacity * sharer.Temperature - heatCapacitySharerToThis * sharer.TemperatureArchived + heatCapacityToSharer*TemperatureArchived) / newSharerHeatCapacity;
                }

                // Thermal energy of the system (self and sharer) is unchanged.

                if (MathF.Abs(oldSharerHeatCapacity) > Atmospherics.MinimumHeatCapacity)
                {
                    if (MathF.Abs(newSharerHeatCapacity / oldSharerHeatCapacity - 1) < 0.1)
                    {
                        TemperatureShare(sharer, Atmospherics.OpenHeatTransferCoefficient);
                    }
                }
            }

            if (temperatureDelta > Atmospherics.MinimumTemperatureToMove || MathF.Abs(movedMoles) > Atmospherics.MinimumMolesDeltaToMove)
            {
                var moles = Moles;
                var theirMoles = sharer.Moles;

                return (TemperatureArchived * (moles + movedMoles)) - (sharer.TemperatureArchived * (theirMoles - movedMoles)) * Atmospherics.R / Volume;
            }

            return 0f;
        }

        public void TemperatureShare(GasMixture sharer, float conductionCoefficient)
        {
            var temperatureDelta = TemperatureArchived - sharer.TemperatureArchived;
            if (MathF.Abs(temperatureDelta) > Atmospherics.MinimumTemperatureDeltaToConsider)
            {
                var heatCapacity = HeatCapacityArchived;
                var sharerHeatCapacity = sharer.HeatCapacityArchived;

                if (sharerHeatCapacity > Atmospherics.MinimumHeatCapacity && heatCapacity > Atmospherics.MinimumHeatCapacity)
                {
                    var heat = conductionCoefficient * temperatureDelta * (heatCapacity * sharerHeatCapacity / (heatCapacity + sharerHeatCapacity));

                    if (!Immutable)
                        Temperature = MathF.Max(Temperature - heat / heatCapacity, Atmospherics.TCMB);

                    if (!sharer.Immutable)
                        sharer.Temperature = MathF.Max(sharer.Temperature + heat / sharerHeatCapacity, Atmospherics.TCMB);
                }
            }
        }

        public int Compare(GasMixture sample)
        {
            var moles = 0;
            var combined = GetCombinedContents(this, sample);

            foreach (var (gas, (quantity, sampleQuantity)) in combined)
            {
                var delta = MathF.Abs(quantity - sampleQuantity);
                if (delta > Atmospherics.MinimumMolesDeltaToMove && (delta > quantity * Atmospherics.MinimumAirRatioToMove))
                    return 1;

                // This actually returned the index of the gas in the original implementation,
                // but since we don't index gasses numerically anymore... Whoops.
            }

            if (moles > Atmospherics.MinimumMolesDeltaToMove)
            {
                var tempDelta = MathF.Abs(Temperature - sample.Temperature);
                if (tempDelta > Atmospherics.MinimumTemperatureDeltaToSuspend)
                    return -1;
            }

            return -2;
        }

        public void Clear()
        {
            if (Immutable) return;
            _contents.Clear();
        }

        public void Multiply(float multiplier)
        {
            if (Immutable) return;
            foreach (var (gas, _) in _contents)
            {
                _contents[gas] *= multiplier;
            }
        }
    }
}
