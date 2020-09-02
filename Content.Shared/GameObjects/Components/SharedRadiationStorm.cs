using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedRadiationPulseComponent : Component
    {
        public override string Name => "RadiationPulse";
        public override uint? NetID => ContentNetIDs.RADIATION_PULSE;
    }

    /// <summary>
    /// For syncing the pulse's lifespan between client and server for the overlay
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RadiationPulseState : ComponentState
    {
        public TimeSpan EndTime { get; }
        public float Range { get; }

        public RadiationPulseState(TimeSpan endTime, float range) : base(ContentNetIDs.RADIATION_PULSE)
        {
            EndTime = endTime;
            Range = range;
        }
    }
}
