using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Robust.Shared.Prototypes;

namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// An electro-relay that disconnects power cables on its own tile from the powernet when it receives a signal.
/// Link it to a signal source (a switch, button, timer, etc.); pulsing the trigger port toggles the relay,
/// severing or restoring the connection of any cables sitting on the same tile. The cables physically stay
/// in place, they just stop conducting while severed.
/// </summary>
[RegisterComponent, Access(typeof(CableRelaySystem))]
public sealed partial class CableRelayComponent : Component
{
    /// <summary>
    /// The sink port that toggles the relay when it receives a signal.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TriggerPort = "Trigger";

    /// <summary>
    /// Which cable types the relay will act on. If null, every cable type (LV, MV, HV) on the tile is affected.
    /// </summary>
    [DataField]
    public List<CableType>? CableTypes;

    /// <summary>
    /// Whether the relay is currently severing the cables on its tile.
    /// </summary>
    [DataField]
    public bool Severed;
}
