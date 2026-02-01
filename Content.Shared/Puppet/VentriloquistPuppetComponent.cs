using Robust.Shared.GameStates;

namespace Content.Shared.Puppet;

/// <summary>
/// This component allows an entity to be a ventriloquist puppet.
/// When used in hand, the entity will speak and the user will be able to hear the entity's voice.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(VentriloquistPuppetSystem))]
public sealed partial class VentriloquistPuppetComponent : Component
{
    /// <summary>
    /// Whether a player's hand is currently inserted into the puppet.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasHandInserted = false;
}
