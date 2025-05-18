using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Adds container for status effect entities that are applied to entity.
/// Is applied automatically upon adding any status effect.
/// Can be used for tracking currently applied status effects.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedStatusEffectsSystem))]
public sealed partial class StatusEffectContainerComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> ActiveStatusEffects = new();
}
