using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component placed on a mob to make it a space ninja, able to use suit and glove powers.
/// Contains ids of all ninja equipment.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSpaceNinjaSystem))]
public sealed partial class SpaceNinjaComponent : Component
{
    /// <summary>
    /// Currently worn suit
    /// </summary>
    [DataField("suit"), AutoNetworkedField]
    public EntityUid? Suit = null;

    /// <summary>
    /// Currently worn gloves
    /// </summary>
    [DataField("gloves"), AutoNetworkedField]
    public EntityUid? Gloves = null;

    /// <summary>
    /// Bound katana, set once picked up and never removed
    /// </summary>
    [DataField("katana"), AutoNetworkedField]
    public EntityUid? Katana = null;
}
