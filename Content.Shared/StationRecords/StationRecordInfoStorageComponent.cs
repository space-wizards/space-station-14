using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

/// <summary>
/// Container for generic station record information.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationRecordInfoStorageComponent : Component
{
    /// <summary> The key stored in this component. </summary>
    [ViewVariables]
    public StationRecordKey? Key;

    /// <summary> The record stored in this component. </summary>
    [ViewVariables]
    public GeneralStationRecord? Record;
}

/// <summary>
/// State for <see cref="StationRecordInfoStorageComponent"/>.
/// </summary>
/// <param name="key">Tuple of station NetEntity and station record id.</param>
/// <param name="record">General station data.</param>
[Serializable, NetSerializable]
public sealed class StationRecordInfoStorageComponentState((NetEntity, uint)? key, GeneralStationRecord? record)
    : ComponentState
{
    /// <summary> Tuple of station NetEntity and station record id. </summary>
    public (NetEntity, uint)? Key = key;

    /// <summary> The record stored in this component. </summary>
    public GeneralStationRecord? Record = record;
}
