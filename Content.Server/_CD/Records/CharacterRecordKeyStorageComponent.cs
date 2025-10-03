namespace Content.Server._CD.Records;

/// <summary>
/// Attached to a mob to remember which character record entry belongs to them.
/// </summary>
[RegisterComponent]
[Access(typeof(CharacterRecordsSystem))]
public sealed partial class CharacterRecordKeyStorageComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public CharacterRecordKey Key;

    public CharacterRecordKeyStorageComponent(CharacterRecordKey key)
    {
        Key = key;
    }
}
