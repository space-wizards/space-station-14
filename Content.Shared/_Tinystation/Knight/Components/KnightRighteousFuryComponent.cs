using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Tinystation.Knight.Components;

/// <summary>
///     Tracks current state of Fury / Berserk for a knight mob.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnightRighteousFuryComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool FuryActive;

    [DataField, AutoNetworkedField]
    public bool BerserkActive;

    /// <summary>
    ///     How long Fury or Berserk lasts.
    /// </summary>
    [DataField]
    public float FuryDuration = 10f;

    [DataField, AutoNetworkedField]
    public TimeSpan FuryEndTime;

    /// <summary>
    ///     Damage multiplier applied to outgoing melee hits while Fury is active.
    ///     Implemented additively via MeleeHitEvent.BonusDamage.
    /// </summary>
    [DataField]
    public float FuryDamageMultiplier = 1.5f;

    /// <summary>
    ///     Speed multiplier while Fury is active. Capped at 1.8 (game max).
    /// </summary>
    [DataField]
    public float FurySpeedMultiplier = 1.8f;

    /// <summary>
    ///     Damage multiplier while Berserk is active.
    /// </summary>
    [DataField]
    public float BerserkDamageMultiplier = 2f;

    /// <summary>
    ///     Speed multiplier while Berserk is active. Capped at 1.8 (game max).
    /// </summary>
    [DataField]
    public float BerserkSpeedMultiplier = 1.8f;

    [DataField]
    public float AutoTriggerThreshold = 0.5f;

    [DataField]
    public float BerserkThreshold = 0.25f;

    [DataField]
    public EntProtoId<InstantActionComponent> FuryAction = "ActionKnightRighteousFury";

    [DataField, AutoNetworkedField]
    public EntityUid? FuryActionEntity;
}
