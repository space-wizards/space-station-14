using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity is used to interact with another entity (<see cref="InteractUsingEvent"/>).
/// The user is the player initiating the interaction or the item used, depending on the TargetUsed datafield.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnInteractUsingComponent : BaseTriggerOnXComponent
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
    /// If false, the trigger user will be the user that initiated the interaction.
    /// If true, the trigger user will the entity that was used to interact.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetUsed = false;
}
