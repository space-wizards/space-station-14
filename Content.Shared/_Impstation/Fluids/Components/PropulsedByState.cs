using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.Fluids;

[Serializable, NetSerializable]
public sealed partial class PropulsedByState : ComponentState
{
    public float WalkSpeedModifier;
    public float SprintSpeedModifier;
    public ulong PredictFingerprint;
}
