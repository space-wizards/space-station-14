using Robust.Shared.Serialization;

namespace Content.Shared.Storage;

[DataDefinition, Serializable, NetSerializable]
public partial record struct ItemStorageLocation
{
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

    public ItemStorageLocation(Angle rotation, Vector2i position)
    {
        Rotation = rotation;
        Position = position;
    }

    public void Rotate(Angle theta)
    {
        Rotation += theta;
    }

    public bool Equals(ItemStorageLocation? other)
    {
        return Rotation == other?.Rotation && Position == other.Value.Position;
    }
};
