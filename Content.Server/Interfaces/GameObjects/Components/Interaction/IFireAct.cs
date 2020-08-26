using System;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    /// <summary>
    ///     This interface gives components behavior when the entity is in a fire.
    /// </summary>
    public interface IFireAct
    {
        void FireAct(FireActEventArgs eventArgs);
    }

    public class FireActEventArgs : EventArgs
    {
        public float Temperature { get; }
        public float Volume { get; }

        public FireActEventArgs(float temperature, float volume)
        {
            Temperature = temperature;
            Volume = volume;
        }
    }
}
