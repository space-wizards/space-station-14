using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[RegisterComponent, NetworkedComponent]
public sealed partial class StationRecordInfoStorageComponent : Component
{
    /// <summary>
    ///     The key stored in this component.
    /// </summary>
    [ViewVariables]
    public StationRecordKey? Key;

    /// <summary>
    ///     The record stored in this component.
    /// </summary>
    [ViewVariables]
    public GeneralStationRecord? Record;
}

[Serializable, NetSerializable]
public sealed class StationRecordInfoStorageComponentState : ComponentState
{
    public (NetEntity, uint)? Key;
    public GeneralStationRecord? Record;

    public StationRecordInfoStorageComponentState((NetEntity, uint)? key, GeneralStationRecord? record)
    {
        Key = key;
        Record = record;
    }
}
