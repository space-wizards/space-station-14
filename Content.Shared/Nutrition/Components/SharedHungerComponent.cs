using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components
{
    [NetworkedComponent()]
    public abstract class SharedHungerComponent : Component
    {
        [ViewVariables]
        public abstract HungerThreshold CurrentHungerThreshold { get; }

        [Serializable, NetSerializable]
        protected sealed class HungerComponentState : ComponentState
        {
            public HungerThreshold CurrentThreshold { get; }

            public HungerComponentState(HungerThreshold currentThreshold)
            {
                CurrentThreshold = currentThreshold;
            }
        }
    }

    [Serializable, NetSerializable]
    public enum HungerThreshold : byte
    {
        Overfed = 1 << 3,
        Okay = 1 << 2,
        Peckish = 1 << 1,
        Starving = 1 << 0,
        Dead = 0,
    }
}
