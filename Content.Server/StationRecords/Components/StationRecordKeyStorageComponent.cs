using Content.Shared.StationRecords;

namespace Content.Server.StationRecords;

[RegisterComponent]
public sealed partial class StationRecordKeyStorageComponent : Component
{
    /// <summary>
    ///     The key stored in this component.
    /// </summary>
    [ViewVariables]
    public StationRecordKey? Key;
}
