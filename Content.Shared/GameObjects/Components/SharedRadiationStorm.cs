using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedRadiationPulseComponent : Component
    {
        public override string Name => "RadiationPulse";
        public override uint? NetID => ContentNetIDs.RADIATION_PULSE;

        public virtual float DPS { get; set; }

        /// <summary>
        /// Radius of the pulse from its position
        /// </summary>
        public virtual float Range { get; set; }

        public virtual TimeSpan EndTime { get; }
    }

    /// <summary>
    /// For syncing the pulse's lifespan between client and server for the overlay
    /// </summary>
    [Serializable, NetSerializable]
    public class RadiationPulseState : ComponentState
    {
        public readonly float DPS;
        public readonly float Range;
        public readonly TimeSpan EndTime;

        public RadiationPulseState(float dps, float range, TimeSpan endTime) : base(ContentNetIDs.RADIATION_PULSE)
        {
            DPS = dps;
            Range = range;
            EndTime = endTime;
        }
    }
}
