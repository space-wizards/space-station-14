using Content.Server.Interfaces.Atmos;
using Content.Shared.Atmos;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Content.Server.Atmos
{
    /// <summary>
    /// Abstract base-class for making performant <see cref="IAtmosphere"/> implementations.
    /// </summary>
    /// <remarks>
    /// This base implementation caches most calculated values for speed. To not invalidate
    /// cached values, call <see cref="UpdateCached"/> whenever you modify some part of the
    /// ideal gas equation - pressure, volume, molar masses, or temperature.
    /// </remarks>
    public abstract class Atmosphere : IAtmosphere
    {
        private readonly Dictionary<Gas, float> _quantities = new Dictionary<Gas, float>();
        private float _temperature;

        public IEnumerable<GasProperty> Gasses => _quantities.Select(p => new GasProperty
        {
            Gas = p.Key,
            Quantity = p.Value,
            PartialPressure = p.Value * PressureRatio
        });

        public abstract float Volume { get; }

        public float Pressure => Quantity * PressureRatio;

        public float Quantity { get; private set; }

        public float Mass => throw new NotImplementedException();

        public float Temperature
        {
            get => _temperature;
            set
            {
                _temperature = value;
                UpdateCached();
            }
        }

        public float PressureRatio { get; private set; }

        public float QuantityOf(Gas gas)
        {
            return _quantities.ContainsKey(gas) ? _quantities[gas] : 0;
        }

        public float Add(Gas gas, float quantity, float temperature)
        {
            if (quantity < 0)
                throw new NotImplementedException();

            if (_quantities.ContainsKey(gas))
            {
                _quantities[gas] += quantity;

                // Convert things to doubles while averaging the temperatures, otherwise
                // small amounts of inaccuracy creep in
                var newTotal = 1d * Quantity + quantity;
                Temperature = (float) ((1d * Temperature * Quantity + 1d * temperature * quantity) / newTotal);
            }
            else
            {
                _quantities[gas] = quantity;
                Temperature = temperature;
            }

            // Setting temperature does the update for us, no need to call it manually

            return quantity;
        }

        public float SetQuantity(Gas gas, float quantity)
        {
            if (quantity < 0)
                throw new ArgumentException("Cannot set a negative quantity of gas", nameof(quantity));

            // Discard tiny amounts of gasses
            if (Math.Abs(quantity) < 0.001)
            {
                _quantities.Remove(gas);
                UpdateCached();
                return 0;
            }

            _quantities[gas] = quantity;
            UpdateCached();
            return quantity;
        }

        public IAtmosphere Take(float volume)
        {
            var taken = Math.Min(volume, Volume);
            var takenProportion = taken / Volume;

            Debug.Assert(0f <= takenProportion && takenProportion <= 1f);

            var mixture = new GasMixture(taken);

            foreach (var gas in _quantities.Keys.ToList())
            {
                var takenQuantity = _quantities[gas] * takenProportion;
                _quantities[gas] -= takenQuantity;

                mixture.SetQuantity(gas, takenQuantity);
            }
            UpdateCached();

            return mixture;
        }

        protected void UpdateCached()
        {
            float q = 0;
            foreach (var value in _quantities.Values) q += value;
            Quantity = q;

            // Remember, R is in Pa - so we divide by 1000 to get kPa
            PressureRatio = IAtmosphere.R * Temperature / Volume / 1000;
        }
    }
}
