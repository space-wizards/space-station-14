using Content.Shared.PDA;
using Robust.Shared.GameStates;

namespace Content.Server.PDA.Ringer;

/// <summary>
/// Opens the store UI when a PDA's ringtone is set to the secret code.
/// Traitors are told the code when greeted.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(RingerSystem))]
public sealed partial class RingerAccessUplinkComponent : Component
{
    /// <summary>
    /// Notes to set ringtone to in order to lock or unlock the uplink.
    /// Set via GenerateUplinkCodeEvent.
    /// </summary>
    [DataField]
    public Note[]? Code;

    /// <summary>
    /// If set, the uplink store can only be opened with the given entity.
    /// </summary>
    [DataField]
    public EntityUid? BoundEntity;
}
