using System.Collections.Generic;
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
    /// Admin console has the functionality of all other types and offers additional controls.
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

    public uint? SelectedIndex { get; set; }

    public Dictionary<uint, CharacterInfo>? CharacterList { get; set; }

    public FullCharacterRecords? SelectedRecord { get; set; }

    public StationRecordsFilter? Filter { get; set; }

    public (SecurityStatus, string?)? SelectedSecurityStatus { get; set; }
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
