using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Triggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class TriggerOnDoAfterComponent : BaseTriggerOnXComponent
{
    [DataField, AutoNetworkedField]
    public bool Consume = true;

    [DataField, AutoNetworkedField]
    public DoAfterParams Params = new();

    [DataField, AutoNetworkedField]
    public bool AttemptRepeat;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public float Delay = 1f;

    [DataField, AutoNetworkedField]
    public float SelfUsePenaltyModifier = 5f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? BeginSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? EndSound;

    [DataField, AutoNetworkedField]
    public LocId? ConditionFailed;

    [DataField, AutoNetworkedField]
    public LocId? ConditionFailedRepeat;

    [DataField, AutoNetworkedField]
    public LocId? SelfUserCompleted;

    [DataField, AutoNetworkedField]
    public LocId? SelfOtherCompleted;

    [DataField, AutoNetworkedField]
    public LocId? UserCompleted;

    [DataField, AutoNetworkedField]
    public LocId? OtherCompleted;

    [DataField, AutoNetworkedField]
    public LocId? ItemsUsedUp;

    [DataField, AutoNetworkedField]
    public LocId? SelfUserStarted;

    [DataField, AutoNetworkedField]
    public LocId? SelfOtherStarted;

    [DataField, AutoNetworkedField]
    public LocId? UserStarted;

    [DataField, AutoNetworkedField]
    public LocId? OtherStarted;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class DoAfterParams
{
    [DataField]
    public bool NeedHand = false;

    [DataField]
    public bool BreakOnHandChange = true;

    [DataField]
    public bool BreakOnDropItem = true;

    [DataField]
    public bool BreakOnMove = false;

    [DataField]
    public bool BreakOnWeightlessMove = true;

    [DataField]
    public float MovementThreshold = 0.3f;

    [DataField]
    public float? DistanceThreshold = 1.5f;

    [DataField]
    public bool BreakOnDamage = false;

    [DataField]
    public FixedPoint2 DamageThreshold = 1;

    [DataField]
    public bool RequireCanInteract = true;

    [DataField]
    public bool BlockDuplicate = true;

    [DataField]
    public bool CancelDuplicate = true;

    [DataField]
    public DuplicateConditions DuplicateCondition = DuplicateConditions.All;
}

[Serializable, NetSerializable]
public sealed partial class TriggerOnDoAfterDoAfterEvent : SimpleDoAfterEvent;
