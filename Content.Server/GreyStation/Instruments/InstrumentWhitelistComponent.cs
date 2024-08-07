using Content.Shared.Whitelist;

namespace Content.Server.GreyStation.Instruments;

/// <summary>
/// Component added to a player that requires any instrument you play to pass a whitelist.
/// </summary>
[RegisterComponent, Access(typeof(InstrumentWhitelistSystem))]
public sealed partial class InstrumentWhitelistComponent : Component
{
    /// <summary>
    /// The whitelist that has to be matched.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// Popup shown when trying to play an instrument that fails the whitelist.
    /// </summary>
    [DataField]
    public LocId FailPopup = "greystation-instrument-fail-popup";
}
