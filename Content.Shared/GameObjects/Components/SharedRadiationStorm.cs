using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedRadiationPulseComponent : Component
    {
        public override string Name => "RadiationPulse";
        public override uint? NetID => ContentNetIDs.RADIATION_PULSE;

        /// <summary>
        /// Radius of the pulse from its position
        /// </summary>
        public float Range => _range;
        private float _range;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _range, "range", 5.0f);
        }
    }
    
    /// <summary>
    /// For syncing the pulse's lifespan between client and server for the overlay
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RadiationPulseMessage : ComponentState
    {
        public TimeSpan EndTime { get; }

        public RadiationPulseMessage(TimeSpan endTime) : base(ContentNetIDs.RADIATION_PULSE)
        {
            EndTime = endTime;
        }
    }
}