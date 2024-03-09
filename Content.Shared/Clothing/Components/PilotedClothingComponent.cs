using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Allows an entity stored in this clothing item to pass inputs to the entity wearing it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PilotedClothingComponent : Component
{
    /// <summary>
    /// Whitelist for entities that are allowed to act as pilots when inside this entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? PilotWhitelist;

    /// <summary>
    /// Should movement input be relayed from the pilot to the target?
    /// </summary>
    [DataField]
    public bool RelayMovement = true;

    /// <summary>
    /// Should click interaction input be relayed from the pilot to the target?
    /// </summary>
    /// <remarks>
    /// This doesn't work very well right now, so it's disabled by default.
    /// If improvements are made to interaction relays, consider using it.
    /// </remarks>
    [DataField]
    public bool RelayInteraction;

    /// <summary>
    /// Reference to the entity contained in the clothing and acting as pilot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Pilot;

    /// <summary>
    /// Reference to the entity wearing this clothing who will be controlled by the pilot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Wearer;

    public bool IsActive => Pilot != null && Wearer != null;
}
