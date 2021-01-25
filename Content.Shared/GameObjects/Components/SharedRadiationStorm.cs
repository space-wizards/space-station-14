using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedRadiationPulseComponent : Component
    {
        /// <summary>
        /// The energy emitted by the radiation pulse. Used to
        /// damage and in power collection (i.e Radiation Collector).
        /// </summary>
        public virtual float Energy { get; set; }
        /// <summary>
        /// Radius of the pulse from its position.
        /// </summary>
        public virtual float Range { get; protected set; }
        /// <summary>
        /// Whether the entity has a limited lifespan.
        /// </summary>
        protected virtual bool Decay { get; set; }
        /// <summary>
        /// When the radiation pulse's entity will be deleted
        /// </summary>
        public virtual TimeSpan EndTime { get; protected set; }
        /// <summary>
        /// When the radiation pulse was initialized.
        /// </summary>
        public virtual TimeSpan StartTime { get; protected set; }
        /// <summary>
        /// The period before emitting radiation.
        /// </summary>
        public virtual float Cooldown { get; protected set; }
    }

    /// <summary>
    /// For syncing the radiation pulse anomaly lifespan
    /// and the client's light behaviour animation
    /// </summary>
    [Serializable, NetSerializable]
    public class RadiationPulseAnomalyState : ComponentState
    {
        public readonly float Range;
        public readonly TimeSpan StartTime;
        public readonly TimeSpan EndTime;

        public RadiationPulseAnomalyState(float range, TimeSpan startTime, TimeSpan endTime) : base(ContentNetIDs.RADIATION_PULSE)
        {
            Range = range;
            StartTime = startTime;
            EndTime = endTime;
        }
    }

    [Serializable, NetSerializable]
    public enum RadiationPulseVisual : byte
    {
        State
    }

    [Serializable, NetSerializable]
    public enum RadiationPulseVisuals : byte
    {
        None,
        Visible
    }
}
