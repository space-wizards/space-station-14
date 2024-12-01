using Robust.Shared.GameStates;

namespace Content.Client._Impstation.Fluids;

[RegisterComponent, NetworkedComponent]
public sealed partial class PropulsedByComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float WalkSpeedModifier = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float SprintSpeedModifier = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public ulong PredictFingerprint = 0;
}
