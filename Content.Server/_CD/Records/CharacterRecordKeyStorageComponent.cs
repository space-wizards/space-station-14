namespace Content.Server._CD.Records;

/// <summary>
/// Stores the key to the entities character records.
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
