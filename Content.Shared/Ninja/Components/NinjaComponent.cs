using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component placed on a mob to make it a space ninja, able to use suit and glove powers.
/// Contains ids of all ninja equipment.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNinjaSystem))]
public sealed partial class NinjaComponent : Component
{
    /// <summary>
    /// Grid entity of the station the ninja was spawned around. Set if spawned naturally by the event.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? StationGrid;

    /// <summary>
    /// Currently worn suit
    /// </summary>
    [ViewVariables]
    public EntityUid? Suit = null;

    /// <summary>
    /// Currently worn gloves
    /// </summary>
    [ViewVariables]
    public EntityUid? Gloves = null;

    /// <summary>
    /// Bound katana, set once picked up and never removed
    /// </summary>
    [ViewVariables]
    public EntityUid? Katana = null;
}
