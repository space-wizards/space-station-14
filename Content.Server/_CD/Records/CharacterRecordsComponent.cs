using Content.Shared._CD.Records;

namespace Content.Server._CD.Records;

/// <summary>
/// The component on the station that stores records after the round starts.
/// </summary>
[RegisterComponent]
[Access(typeof(CharacterRecordsSystem))]
public sealed partial class CharacterRecordsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, FullCharacterRecords> Records = new();
}
