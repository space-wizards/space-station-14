using Content.Shared.Actions;
﻿using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// An action that targets an entity.
/// Requires <see cref="TargetActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityTargetActionComponent : Component
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField(required: true), NonSerialized, AutoNetworkedField]
    public EntityTargetActionEvent? Event;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    [DataField, AutoNetworkedField]
    public bool CanTargetSelf = true;
}
