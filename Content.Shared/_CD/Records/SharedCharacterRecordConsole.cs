using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;

namespace Content.Shared._CD.Records;

[Serializable, NetSerializable]
public enum CharacterRecordConsoleKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum RecordConsoleType : byte
{
    Security,
    Medical,
    Employment,
    /// <summary>
    /// Admin console has the functionality of all other types and has some additional admin related functionality
    /// </summary>
    Admin
}

[Serializable, NetSerializable]
public sealed class CharacterRecordConsoleState : BoundUserInterfaceState
{
    [Serializable, NetSerializable]
    public struct CharacterInfo
    {
        public string CharacterDisplayName;
        public uint? StationRecordKey;
    }

    public RecordConsoleType ConsoleType { get; set; }

    /// <summary>
    /// Character selected in the console
    /// </summary>
    public uint? SelectedIndex { get; set; } = null;

    /// <summary>
    /// List of names+station record keys to display in the listing
    /// </summary>
    public Dictionary<uint, CharacterInfo>? CharacterList { get; set; }

    /// <summary>
    /// The contents of the selected record
    /// </summary>
    public FullCharacterRecords? SelectedRecord { get; set; } = null;

    public StationRecordsFilter? Filter { get; set; } = null;

    /// <summary>
    /// Security status of the selected record
    /// </summary>
    public (SecurityStatus, string?)? SelectedSecurityStatus = null;
}

[Serializable, NetSerializable]
public sealed class CharacterRecordsConsoleFilterMsg : BoundUserInterfaceMessage
{
    public readonly StationRecordsFilter? Filter;

    public CharacterRecordsConsoleFilterMsg(StationRecordsFilter? filter)
    {
        Filter = filter;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterRecordConsoleSelectMsg : BoundUserInterfaceMessage
{
    public readonly uint? CharacterRecordKey;

    public CharacterRecordConsoleSelectMsg(uint? characterRecordKey)
    {
        CharacterRecordKey = characterRecordKey;
    }
}
