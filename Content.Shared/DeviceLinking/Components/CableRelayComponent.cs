using Content.Shared.DeviceLinking.Systems;
using Content.Shared.Power;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// An electro-relay machine that sits on a power cable tile. When switched on - by a linked signal source or from
/// its interface - it severs the powernet of the selected cable types running through its tile, without removing
/// the cables. Power is needed to switch it on; once thrown the break latches to the switch + anchoring, so the
/// relay can cut the very line that feeds it. Unanchoring or destroying it releases the cables.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CableRelayComponent : Component
{
    /// <summary>
    /// The sink port that toggles the relay when it receives a signal.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TriggerPort = "Trigger";

    /// <summary>
    /// Which cable types the relay severs while active. Configurable from the interface. Defaults to HV and MV;
    /// low voltage is opt-in since the relay is usually powered through an LV line and could otherwise cut itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<CableType> AffectedTypes = new() { CableType.HighVoltage, CableType.MediumVoltage };

    /// <summary>
    /// Whether the player has switched the relay on. The cables are severed while this is set <i>and</i> the relay
    /// is anchored; the node connections are always re-derived from these facts, so the relay can never end up out
    /// of sync with the cables on its tile.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Severed;
}
