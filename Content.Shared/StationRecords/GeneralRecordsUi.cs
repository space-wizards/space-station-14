using Robust.Shared.Serialization;

namespace Content.Shared.StationRecords;

[Serializable, NetSerializable]
public enum GeneralStationRecordConsoleKey : byte
{
    Key
}

/// <summary>
/// Select a specific crewmember's record, or deselect.
/// Used by any kind of records console including general and criminal.
/// </summary>
[Serializable, NetSerializable]
[Obsolete("Make your station records UI properly predicted")]
public sealed class SelectStationRecord : BoundUserInterfaceMessage
{
    public readonly uint? SelectedKey;

    public SelectStationRecord(uint? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}

[Serializable, NetSerializable]
public sealed class DeleteStationRecord : BoundUserInterfaceMessage
{
    public DeleteStationRecord(uint id)
    {
        Id = id;
    }

    public readonly uint Id;
}
