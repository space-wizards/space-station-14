using System.Numerics;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public sealed partial class Decal
    {
        // if these are made not-readonly, then decal grid state handling needs to be updated to clone decals.
        [DataField] public Vector2 Coordinates = Vector2.Zero;
        [DataField] public ProtoId<DecalPrototype> Id = string.Empty;
        [DataField] public Color? Color;
        [DataField] public Angle Angle = Angle.Zero;
        [DataField] public int ZIndex;
        [DataField] public bool Cleanable;

        public Decal() {}
        public Decal(ProtoId<DecalPrototype> id) {
            Id = id;
        }

        public Decal(Vector2 coordinates, ProtoId<DecalPrototype> id, Color? color, Angle angle, int zIndex, bool cleanable)
        {
            Coordinates = coordinates;
            Id = id;
            Color = color;
            Angle = angle;
            ZIndex = zIndex;
            Cleanable = cleanable;
        }

        public Decal WithCoordinates(Vector2 coordinates) => new(coordinates, Id, Color, Angle, ZIndex, Cleanable);
        public Decal WithId(ProtoId<DecalPrototype> id) => new(Coordinates, id, Color, Angle, ZIndex, Cleanable);
        public Decal WithColor(Color? color) => new(Coordinates, Id, color, Angle, ZIndex, Cleanable);
        public Decal WithRotation(Angle angle) => new(Coordinates, Id, Color, angle, ZIndex, Cleanable);
        public Decal WithZIndex(int zIndex) => new(Coordinates, Id, Color, Angle, zIndex, Cleanable);
        public Decal WithCleanable(bool cleanable) => new(Coordinates, Id, Color, Angle, ZIndex, cleanable);
    }
}
