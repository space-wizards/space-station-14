using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Construction.Components;

/// <summary>
/// Any entities anchored to this spot can't have overlapping directions to this entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AnchorUniqueDirectionComponent : Component
{
    /// <summary>
    /// Only considers cardinal directions.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("directions", required: true)]
    public DirectionFlag Directions;

    /// <summary>
    /// Whitelist to check for entities on the anchor spot.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("whitelist", required: true), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
