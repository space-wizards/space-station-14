using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffectNew.Components;

/// <summary>
/// Adds container for status effect entities that are applied to entity.
/// Is applied automatically upon adding any status effect.
/// Can be used for tracking currently applied status effects.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(StatusEffectsSystem))]
public sealed partial class StatusEffectContainerComponent : Component
{
    public const string ContainerId = "status-effects";

    /// <summary>
    /// The actual container holding references to the active status effects
    /// </summary>
    [ViewVariables]
    public Container? ActiveStatusEffects;
}
