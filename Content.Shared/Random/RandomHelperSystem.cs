using System.Numerics;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Random;

/// <summary>
///     System containing various content-related random helpers.
/// </summary>
public sealed class RandomHelperSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public void RandomOffset(EntityUid entity, float minX, float maxX, float minY, float maxY)
    {
        var randomX = _random.NextFloat() * (maxX - minX) + minX;
        var randomY = _random.NextFloat() * (maxY - minY) + minY;
        var offset = new Vector2(randomX, randomY);

        var xform = Transform(entity);
        _transform.SetLocalPosition(entity, xform.LocalPosition + offset, xform);
    }

    public void RandomOffset(EntityUid entity, float min, float max)
    {
        RandomOffset(entity, min, max, min, max);
    }

    public void RandomOffset(EntityUid entity, float value)
    {
        RandomOffset(entity, -value, value);
    }
}
