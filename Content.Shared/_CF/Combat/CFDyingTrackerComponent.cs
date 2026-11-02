using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Present while a combatant has the PF2e dying condition. Drives recovery checks every round (6s).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CFDyingTrackerComponent : Component
{
    public const float RecoveryIntervalSeconds = 6f;
    public const int DeathValue = 4;

    /// <summary>Flat check DC base; full DC is <c>BaseRecoveryDc + Dying</c>.</summary>
    public const int BaseRecoveryDc = 10;

    /// <summary>Medicine Stabilize DC base; full DC is <c>BaseStabilizeDc + Dying</c>.</summary>
    public const int BaseStabilizeDc = 5;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRecoveryCheck;
}
