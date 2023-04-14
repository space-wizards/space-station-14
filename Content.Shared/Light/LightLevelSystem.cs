using System.Linq;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Light;

public sealed class LightLevelSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private bool Ignored(EntityUid ignored)
    {
        return !Transform(ignored).Anchored;
    }

    [PublicAPI]
    public float GetLightLevel(EntityUid uid, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref xform))
            return 0f;
        var pos = _transform.GetWorldPosition(xform);
        return GetLightLevel(pos, xform.MapID);
    }

    [PublicAPI]
    public float GetLightLevel(Vector2 pos, MapId map)
    {
        // todo: in the future, this should support light on planet maps.
        var lightLevel = 0f;

        var validLights = new HashSet<(SharedPointLightComponent, TransformComponent)>();
        foreach (var (light, xform) in EntityQuery<SharedPointLightComponent, TransformComponent>(true))
        {
            if (!light.Enabled)
                continue;

            if (xform.MapID != map)
                continue;
            validLights.Add((light, xform));
        }

        // quit while we're ahead.
        if (validLights.Count == 0)
            return lightLevel;

        foreach (var (light, xform) in validLights)
        {
            var lightRot = _transform.GetWorldRotation(xform);
            var lightPos = _transform.GetWorldPosition(xform) + lightRot.RotateVec(light.Offset);
            var direction = pos - lightPos;
            var length = direction.Length;
            if (length > light.Radius)
                continue;

            if (light.CastShadows && !direction.LengthSquared.Equals(0f))
            {
                var ray = new CollisionRay(lightPos, direction.Normalized, (int) CollisionGroup.Opaque);
                var rayResults = _physics.IntersectRayWithPredicate(map, ray, length, Ignored, false).ToList();
                if (rayResults.Count != 0)
                    continue;
            }

            var strength = light.Energy * (1f - length / light.Radius);
            lightLevel += strength;
        }

        return lightLevel;
    }
}
