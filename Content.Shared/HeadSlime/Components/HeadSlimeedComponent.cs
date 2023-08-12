using Robust.Shared.Serialization;

namespace Content.Shared.HeadSlime.Components
{
    [Access(typeof(SharedHeadSlimeSystem))]
    [RegisterComponent]
    public sealed class HeadSlimeedComponent : Component
    {
        [ViewVariables]
        public bool HeadSlimeed { get; set; } = false;
    }

    [Serializable, NetSerializable]
    public enum HeadSlimeedVisuals
    {
        HeadSlimeed,
    }
}
