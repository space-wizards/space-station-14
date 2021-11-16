using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    [DataDefinition]
    public class Decal
    {
        [DataField("coordinates")] public readonly Vector2 Coordinates;
        [DataField("id")] public readonly string Id;
        [DataField("color")] public readonly Color? Color;
        [DataField("angle")] public readonly Angle Angle;
        [DataField("zIndex")] public readonly int ZIndex;

        public Decal(Vector2 coordinates, string id, Color? color, Angle angle, int zIndex)
        {
            Coordinates = coordinates;
            Id = id;
            Color = color;
            Angle = angle;
            ZIndex = zIndex;
        }

        public Decal WithCoordinates(Vector2 coordinates) => new(coordinates, Id, Color, Angle, ZIndex);
        public Decal WithId(string id) => new(Coordinates, id, Color, Angle, ZIndex);
        public Decal WithColor(Color? color) => new(Coordinates, Id, color, Angle, ZIndex);
    }
}
