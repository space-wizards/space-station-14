using System;
using Content.Server.Atmos;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    public interface ITemperatureExpose
    {
        void TemperatureExpose(TemperatureExposeEventArgs eventArgs);
    }

    public class TemperatureExposeEventArgs : EventArgs
    {
        public GasMixture Air { get; }
        public float Temperature { get; }
        public float Volume { get; }

        public TemperatureExposeEventArgs(GasMixture air, float temperature, float volume)
        {
            Air = air;
            Temperature = temperature;
            Volume = volume;
        }
    }
}
