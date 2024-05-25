using Content.Shared.Actions;
﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// An action that targets an entity.
/// Requires <see cref="TargetActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem))]
[EntityCategory("Actions")]
public sealed partial class EntityTargetActionComponent : Component
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField(required: true), NonSerialized]
    public EntityTargetActionEvent? Event;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public bool CanTargetSelf = true;
}
