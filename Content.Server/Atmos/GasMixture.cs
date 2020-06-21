using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Atmos
{
    /// <summary>
    /// A general-purposes, variable volume gas mixture.
    /// </summary>
    public class GasMixture : Atmosphere
    {
        private float _volume;

        public override float Volume
        {
            get => _volume;
        }

        public GasMixture(float volume)
        {
            _volume = volume;
            UpdateCached();
        }

        // Blame C# for not allowing you to add setters to overridden properties
        // Despite it being raised as an issue >2 years ago

        /// <summary>
        /// Set the volume of the gas mixture. Will alter pressure.
        /// </summary>
        /// <param name="value">The new volume of the gas mixture.</param>
        public void SetVolume(float value)
        {
            _volume = value;
            UpdateCached();
        }
    }
}
