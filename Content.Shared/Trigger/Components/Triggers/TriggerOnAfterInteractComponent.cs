using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when using an entity on another entity. Raised on the used entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnAfterInteractComponent : BaseTriggerOnXComponent
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
}
