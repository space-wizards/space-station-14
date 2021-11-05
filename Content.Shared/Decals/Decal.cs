using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Decals
{
    [Serializable, NetSerializable]
    public record Decal(Vector2 Coordinates, string Id, Color? Color);
}
