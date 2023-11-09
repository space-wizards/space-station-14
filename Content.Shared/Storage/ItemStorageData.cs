namespace Content.Shared.Storage;

[DataDefinition]
public partial record struct ItemStorageData
{
    /// <summary>
    /// The item being stored.
    /// </summary>
    [DataField]
    public EntityUid ItemEntity;

    /// <summary>
    /// The rotation of the piece in storage.
    /// </summary>
    [DataField]
    public Angle Rotation;

    /// <summary>
    /// Where the item is located in storage.
    /// </summary>
    [DataField]
    public Vector2i Position;

    public ItemStorageData(EntityUid itemEntity, Angle rotation, Vector2i position)
    {
        ItemEntity = itemEntity;
        Rotation = rotation;
        Position = position;
    }
};
