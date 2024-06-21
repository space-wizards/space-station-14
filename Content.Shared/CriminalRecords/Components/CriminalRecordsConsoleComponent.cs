using Content.Shared.CriminalRecords.Systems;
using Content.Shared.Radio;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Shared.CriminalRecords.Components;

/// <summary>
/// A component for Criminal Record Console storing an active station record key and a currently applied filter
/// </summary>
[RegisterComponent]
[Access(typeof(SharedCriminalRecordsConsoleSystem))]
public sealed partial class CriminalRecordsConsoleComponent : Component
{
    /// <summary>
    /// Currently active station record key.
    /// There is no station parameter as the console uses the current station.
    /// </summary>
    /// <remarks>
    /// TODO: in the future this should be clientside instead of something players can fight over.
    /// Client selects a record and tells the server the key it wants records for.
    /// Server then sends a state with just the records, not the listing or filter, and the client updates just that.
    /// I don't know if it's possible to have multiple bui states right now.
    /// </remarks>
    [DataField]
    public uint? ActiveKey;

    /// <summary>
    /// Currently applied filter.
    /// </summary>
    [DataField]
    public StationRecordsFilter? Filter;

    /// <summary>
    /// Channel to send messages to when someone's status gets changed.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> SecurityChannel = "Security";

    /// <summary>
    /// Max length of arrest and crime history strings.
    /// </summary>
    [DataField]
    public uint MaxStringLength = 256;
}
