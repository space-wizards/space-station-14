using Content.Shared.Actions;
﻿using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Used on action entities to define an action that triggers when targeting an entity coordinate.
/// Can be combined with <see cref="EntityTargetActionComponent"/>, see its docs for more information.
/// </summary>
/// <remarks>
/// Requires <see cref="TargetActionComponent"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem))]
[EntityCategory("Actions")]
public sealed partial class WorldTargetActionComponent : Component
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField(required: true), NonSerialized]
    public WorldTargetActionEvent? Event;
}
