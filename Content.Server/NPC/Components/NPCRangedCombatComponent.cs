using Content.Server.NPC.Systems;
using Robust.Shared.Audio;

namespace Content.Server.NPC.Components;

/// <summary>
/// Added to an NPC doing ranged combat.
/// </summary>
[RegisterComponent]
public sealed partial class NPCRangedCombatComponent : Component
{
    [ViewVariables]
    public EntityUid Target;

    [ViewVariables]
    public CombatStatus Status = CombatStatus.Normal;

    // Most of the below is to deal with turrets.

    /// <summary>
    /// If null it will instantly turn.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public Angle? RotationSpeed;

    /// <summary>
    /// Maximum distance, between our rotation and the target's, to consider shooting it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle AccuracyThreshold = Angle.FromDegrees(30);

    /// <summary>
    /// How long until the last line of sight check.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float LOSAccumulator = 0f;

    /// <summary>
    ///  Is the target still considered in LOS since the last check.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool TargetInLOS = false;

    /// <summary>
    /// Delay after target is in LOS before we start shooting.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ShootDelay = 0.2f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ShootAccumulator;

    /// <summary>
    /// Sound to play if the target enters line of sight.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? SoundTargetInLOS;
}
