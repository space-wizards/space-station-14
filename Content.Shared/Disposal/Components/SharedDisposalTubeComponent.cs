using Robust.Shared.Serialization;

namespace Content.Shared.Disposal.Components
{
    [Serializable, NetSerializable]
    public enum DisposalTubeVisuals
    {
        VisualState
    }

    [Serializable, NetSerializable]
    public enum DisposalTubeVisualState
    {
        Free = 0,
        Anchored,
    }
}
