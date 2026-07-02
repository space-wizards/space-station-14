using Robust.Shared.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CablePlacerComponent : Component
{
    /// <summary>
    /// The structure prototype for the cable coil to place.
    /// </summary>
    [DataField]
    public EntProtoId? CablePrototypeID = "CableHV";

    /// <summary>
    /// What kind of wire prevents placing this wire over it as CableType.
    /// </summary>
    [DataField("blockingWireType")]
    public CableType BlockingCableType = CableType.HighVoltage;

    /// <summary>
    /// Blacklist for things the cable cannot be placed over. For things that arent cables with CableTypes.
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist = new();

    /// <summary>
    /// Whether the placed cable should go over tiles or not.
    /// </summary>
    [DataField]
    public bool OverTile;
}
