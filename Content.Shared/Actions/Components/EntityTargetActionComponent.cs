using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Used on action entities to define an action that triggers when targeting an entity.
/// If used with <see cref="WorldTargetActionComponent"/>, the event here can be set to null and <c>Optional</c> should be set.
/// Then <see cref="WorldActionEvent"> can have <c>TargetEntity</c> optionally set to the client's hovered entity, if it is valid.
/// Using entity-world targeting like this will always give coords, but doesn't need to have an entity.
/// </summary>
/// <remarks>
/// Requires <see cref="TargetActionComponent"/>.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem))]
[EntityCategory("Actions")]
[AutoGenerateComponentState]
public sealed partial class EntityTargetActionComponent : Component
{
    /// <summary>
    /// The local-event to raise when this action is performed.
    /// If this is null entity-world targeting is done as specified on the component doc.
    /// </summary>
    [DataField, NonSerialized]
    public EntityTargetActionEvent? Event;

    /// <summary>
    /// Determines which entities are valid targets for this action.
    /// </summary>
    /// <remarks>No whitelist check when null.</remarks>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Determines which entities cannot be valid targets for this action, even if matching the whitelist.
    /// </summary>
    /// <remarks>No blacklist check when null.</remarks>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Whether this action considers the user as a valid target entity when using this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanTargetSelf = true;

    /// <summary>
    /// Whether to make the user face towards the direction where they targeted this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RotateOnUse = true;
}
