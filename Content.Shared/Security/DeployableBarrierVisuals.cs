using Robust.Shared.Serialization;

namespace Content.Shared.Security
{
    [Serializable, NetSerializable]
    public enum DeployableBarrierVisuals : byte
    {
        State
    }


    [Serializable, NetSerializable]
    public enum DeployableBarrierState : byte
    {
        Idle,
        Deployed
    }
}
