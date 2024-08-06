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
    /// Reference to the entity contained in the clothing and acting as pilot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Pilot;

    /// <summary>
    /// Reference to the entity wearing this clothing who will be controlled by the pilot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;

    public bool IsActive => Pilot != null && Wearer != null;
}
