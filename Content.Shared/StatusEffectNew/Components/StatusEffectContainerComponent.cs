using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Adds container for status effect entities that are applied to entity.
/// Is applied automatically upon adding any status effect.
/// Can be used for tracking currently applied status effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStatusEffectsSystem))]
public sealed partial class StatusEffectContainerComponent : Component
{
    [DataField]
    public HashSet<EntityUid> ActiveStatusEffects = new();
}

[Serializable, NetSerializable]
public sealed class StatusEffectContainerComponentState(HashSet<NetEntity> activeStatusEffects) : ComponentState
{
    public readonly HashSet<NetEntity> ActiveStatusEffects = activeStatusEffects;
}
