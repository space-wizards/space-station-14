using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the owner uses another entity to interact with another entity (<see cref="UserInteractUsingEvent"/>).
/// The trigger user is the interacted entity or the item used, depending on the TargetUsed datafield.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUserInteractUsingComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// Whitelist of entities that can be used to trigger this component.
    /// </summary>
    /// <remarks>No whitelist check when null.</remarks>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist of entities that cannot be used to trigger this component.
    /// </summary>
    /// <remarks>No blacklist check when null.</remarks>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// If false, the trigger user will be the entity that got interacted with.
    /// If true, the trigger user will the entity that was used to interact.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetUsed = false;

    /// <summary>
    /// Whether the interaction should be marked as handled after it happens.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Handle = true;
}
