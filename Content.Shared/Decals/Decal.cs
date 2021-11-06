using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    public record Decal(Vector2 Coordinates, string Id, Color? Color, Angle Angle, int ZIndex)
    {
        public Decal WithCoordinates(Vector2 coordinates) => new(coordinates, Id, Color, Angle, ZIndex);
        public Decal WithId(string id) => new(Coordinates, id, Color, Angle, ZIndex);
        public Decal WithColor(Color? color) => new(Coordinates, Id, color, Angle, ZIndex);
    }
}
