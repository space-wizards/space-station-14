using Content.Server.CriminalRecords.Systems;
using Content.Shared.Radio;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.CriminalRecords.Components;

/// <summary>
/// A component for Criminal Record Console storing an active station record key and a currently applied filter
/// </summary>
[RegisterComponent]
[Access(typeof(GeneralCriminalRecordConsoleSystem))]
public sealed class GeneralCriminalRecordConsoleComponent : Component
{
    /// <summary>
    /// Currently active station record key
    /// </summary>
    [DataField("activeKey"), ViewVariables]
    public StationRecordKey? ActiveKey;

    /// <summary>
    /// Currently applied filter
    /// </summary>
    [DataField("filter"), ViewVariables]
    public GeneralStationRecordsFilter? Filter;

    [DataField("securityChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string SecurityChannel = "Security";

    public bool HasAccess = false;
}
