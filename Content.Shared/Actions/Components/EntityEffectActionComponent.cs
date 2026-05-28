using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// An action component that applies a list of entity effects to the target when performed.
/// Requires <see cref="EntityTargetActionComponent"/> and <see cref="ActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(EntityEffectActionSystem))]
public sealed partial class EntityEffectActionComponent : Component
{
    /// <summary>
    /// List of entity effects to apply to the target when this action is performed.
    /// </summary>
    [DataField(required: true)]
    public List<EntityEffect> Effects = [];
}

/// <summary>
/// Event raised when an entity effect action is performed.
/// </summary>
public sealed partial class EntityEffectActionEvent : EntityTargetActionEvent;
