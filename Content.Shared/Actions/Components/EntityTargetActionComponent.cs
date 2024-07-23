using Content.Shared.Actions;
﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Used on action entities to define an action that triggers when targeting an entity.
/// </summary>
/// <remarks>
/// Requires <see cref="TargetActionComponent"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem))]
[EntityCategory("Actions")]
public sealed partial class EntityTargetActionComponent : Component
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField(required: true), NonSerialized]
    public EntityTargetActionEvent? Event;

    /// <summary>
    /// Determines which entities are valid targets for this action.
    /// </summary>
    /// <remarks>No whitelist check when null.</remarks>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whether this action considers the user as a valid target entity when using this action.
    /// </summary>
    [DataField]
    public bool CanTargetSelf = true;
}
