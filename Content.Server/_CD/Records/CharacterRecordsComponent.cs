using System.Collections.Generic;
using Content.Shared._CD.Records;
using Robust.Shared.GameObjects;

namespace Content.Server._CD.Records;

/// <summary>
/// Stores every player's record for a given station during the round.
/// </summary>
[RegisterComponent]
[Access(typeof(CharacterRecordsSystem))]
public sealed partial class CharacterRecordsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<uint, FullCharacterRecords> Records = new();

    [ViewVariables(VVAccess.ReadOnly)]
    private uint _nextKey = 1;

    /// <summary>
    /// Generate a fresh key for a new record entry.
    /// </summary>
    public uint CreateNewKey()
    {
        return _nextKey++;
    }
}

public sealed record CharacterRecordKey
{
    public EntityUid Station { get; init; }
    public uint Index { get; init; }
}
