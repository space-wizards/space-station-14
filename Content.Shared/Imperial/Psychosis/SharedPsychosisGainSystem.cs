using Robust.Shared.Serialization;

namespace Content.Shared.Traits.Assorted
{
    public abstract class SharedPsychosisGainSystem : EntitySystem
    {
    }
    [Serializable, NetSerializable]
    public sealed class Stats : EntityEventArgs
    {
        public bool Gained = false;

        public float Status = 0f;

        public float Resist = 1f;

        public NetEntity PsychosisGain = default!;
        public Stats(bool gained, float resist, float status, NetEntity component)
        {
            Gained = gained;
            Status = status;
            Resist = resist;
            PsychosisGain = component;
        }
    }
}
