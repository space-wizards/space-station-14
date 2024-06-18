using System.Numerics;

namespace Content.Shared.Camera;

[ByRefEvent]
public record struct GetEyeOffsetEvent(Vector2 Offset);
