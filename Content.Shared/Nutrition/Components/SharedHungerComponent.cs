using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components
{
    [NetworkedComponent, AutoGenerateComponentState]
    public abstract class SharedHungerComponent : Component
    {
        [ViewVariables]
        [AutoNetworkedField]
        public HungerThreshold CurrentHungerThreshold { get; set; }
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
