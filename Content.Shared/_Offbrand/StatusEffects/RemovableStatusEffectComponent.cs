using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(RemovableStatusEffectSystem))]
public sealed partial class RemovableStatusEffectComponent : Component
{
    /// <summary>
    /// Verb message to display when removing this status effect
    /// </summary>
    [DataField(required: true)]
    public LocId Verb;

    /// <summary>
    /// How long the status effect removal takes
    /// </summary>
    [DataField]
    public TimeSpan RemovalTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Entity to spawn when removed
    /// </summary>
    [DataField]
    public EntProtoId? SpawnOnRemove;

    [DataField]
    public LocId? SelfUserCompleted;

    [DataField]
    public LocId? SelfOtherCompleted;

    [DataField]
    public LocId? UserCompleted;

    [DataField]
    public LocId? OtherCompleted;

    [DataField]
    public LocId? SelfUserStarted;

    [DataField]
    public LocId? SelfOtherStarted;

    [DataField]
    public LocId? UserStarted;

    [DataField]
    public LocId? OtherStarted;
}

[Serializable, NetSerializable]
public sealed partial class RemoveStatusEffectEvent : SimpleDoAfterEvent;
