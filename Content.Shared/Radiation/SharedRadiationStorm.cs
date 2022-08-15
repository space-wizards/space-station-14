using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation
{
    [NetworkedComponent()]
    public abstract class SharedRadiationPulseComponent : Component
    {
        [DataField("draw")]
        public bool Draw = true;

        [DataField("sound")]
        public SoundSpecifier Sound = new SoundCollectionSpecifier("RadiationPulse");
    }

    /// <summary>
    /// For syncing the pulse's lifespan between client and server for the overlay
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RadiationPulseState : ComponentState
    {
        // not networking RadsPerSecond because damage is only ever dealt by server-side systems.

        public readonly float Range;
        public readonly bool Draw;
        public readonly bool Decay;
        public readonly TimeSpan StartTime;
        public readonly TimeSpan EndTime;

        public RadiationPulseState(float range, bool draw, bool decay, TimeSpan startTime, TimeSpan endTime)
        {
            Range = range;
            Draw = draw;
            Decay = decay;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}
