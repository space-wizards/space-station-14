using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when an entity is used to interact with another entity (<see cref="InteractUsingEvent"/>).
/// User is the player initiating the interaction.
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
}
