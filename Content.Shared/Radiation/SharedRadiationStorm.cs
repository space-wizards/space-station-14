using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation
{
    [NetworkedComponent()]
    public abstract class SharedRadiationPulseComponent : Component
    {
        public override string Name => "RadiationPulse";

        public virtual float RadsPerSecond { get; set; }

        /// <summary>
        /// Radius of the pulse from its position
        /// </summary>
        public virtual float Range { get; set; }

        public virtual bool Decay { get; set; }
        public virtual bool Draw { get; set; }

        public virtual TimeSpan StartTime { get; }
        public virtual TimeSpan EndTime { get; }
    }

    /// <summary>
    /// For syncing the pulse's lifespan between client and server for the overlay
    /// </summary>
    [Serializable, NetSerializable]
    public class RadiationPulseState : ComponentState
    {
        public readonly float RadsPerSecond;
        public readonly float Range;
        public readonly bool Draw;
        public readonly bool Decay;
        public readonly TimeSpan StartTime;
        public readonly TimeSpan EndTime;

        public RadiationPulseState(float radsPerSecond, float range, bool draw, bool decay, TimeSpan startTime, TimeSpan endTime)
        {
            RadsPerSecond = radsPerSecond;
            Range = range;
            Draw = draw;
            Decay = decay;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
