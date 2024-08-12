using Robust.Shared.Serialization;

namespace Content.Shared.Storage;

[DataDefinition, Serializable, NetSerializable]
public partial record struct ItemStorageLocation
{
    /// <summary>
    /// The rotation, stored a cardinal direction in order to reduce rounding errors.
    /// </summary>
    [DataField("_rotation")]
    public Direction Direction;

    /// <summary>
    /// The rotation of the piece in storage.
    /// </summary>
    public Angle Rotation
    {
        get => Direction.ToAngle();
        set => Direction = value.GetCardinalDir();
    }

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

    public bool Equals(ItemStorageLocation? other)
    {
        return Rotation == other?.Rotation &&
               Position == other.Value.Position;
    }
};
