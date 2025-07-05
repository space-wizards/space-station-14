using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Den.Vampire.Events;

public sealed partial class VampireDrinkBloodAbility : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class VampireDrinkBloodAbilityDoAfter : SimpleDoAfterEvent;
