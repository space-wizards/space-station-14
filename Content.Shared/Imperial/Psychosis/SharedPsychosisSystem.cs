using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Traits.Assorted
{
    public abstract class SharedPsychosisSystem : EntitySystem
    {
    }
    [Serializable, NetSerializable]
    public sealed class StageChange : EntityEventArgs
    {
        public int Stage = 1;

        public NetEntity Psychosis = default!;
        public StageChange(int stage, NetEntity component)
        {
            Stage = stage;
            Psychosis = component;
        }
    }
}
