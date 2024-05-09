using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component placed on a mob to make it a space ninja, able to use suit and glove powers.
/// Contains ids of all ninja equipment and the game rule.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSpaceNinjaSystem))]
public sealed partial class SpaceNinjaComponent : Component
{
    /// <summary>
    /// The ninja game rule that spawned this ninja.
    /// </summary>
    [DataField("rule")]
    public EntityUid? Rule;

    /// <summary>
    /// Currently worn suit
    /// </summary>
    [DataField("suit"), AutoNetworkedField]
    public EntityUid? Suit;

    /// <summary>
    /// Currently worn gloves
    /// </summary>
    [DataField("gloves"), AutoNetworkedField]
    public EntityUid? Gloves;

    /// <summary>
    /// Bound katana, set once picked up and never removed
    /// </summary>
    [DataField("katana"), AutoNetworkedField]
    public EntityUid? Katana;

    /// <summary>
    /// Objective to complete after calling in a threat.
    /// </summary>
    [DataField]
    public EntProtoId TerrorObjective = "TerrorObjective";

    /// <summary>
    /// Objective to complete after setting everyone to arrest.
    /// </summary>
    [DataField]
    public EntProtoId MassArrestObjective = "MassArrestObjective";

    /// <summary>
    /// Objective to complete after the spider charge detonates.
    /// </summary>
    [DataField]
    public EntProtoId SpiderChargeObjective = "SpiderChargeObjective";
}
