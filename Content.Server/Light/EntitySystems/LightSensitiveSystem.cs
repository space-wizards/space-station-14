using Content.Shared.Light.EntitySystems;
using Content.Shared.Light.Components;
using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Light;
using Robust.Shared.Timing;

namespace Content.Server.Light.EntitySystems;

// public sealed class LightSensitiveSystem : SharedLightSensitiveSystem
// {
//     [Dependency] private readonly IGameTiming _gameTiming = default!;
//     [Dependency] private readonly SharedTransformSystem _transform = default!;
//     //[Dependency] private readonly LightLevelSystem _level = default!;

//     private const float DefaultCooldown = 1f;

//     // public override void Update(float frameTime)
//     // {
//     //     base.Update(frameTime);
//     //     var query = EntityQueryEnumerator<LightSensitiveComponent>();
//     //     while (query.MoveNext(out var uid, out var component))
//     //     {
//     //         TryGetLightLevel(uid, out var light_level);
//     //     }
//     // }

//     /// <summary>
//     ///     Returns the illumination level of an entity. If the entity doesn't have a LightSensitiveComponent, one will be added to store the light level.
//     ///     If the entity hasn't had its light level calculated, or it hasn't been updated in the last _cooldown seconds, it will be re-calculated.
//     ///     Subsequent calls to this method within a short period will return the previously calculated light level, unless the forceUpdate parameter is true.
//     /// </summary>
//     /// <remarks>
//     ///     If you're designing a system that depends on the light level of an entity, you should create a const variable that the system will
//     ///     use for the cooldown. I anticipate as time goes on, more systems will use this same function for potentially reasons and all have different cooldowns.
//     ///     I really hope I don't have to deal with the fallout of this design choice.
//     /// </remarks>
//     /// <param name="uid">Entity UID to check.</param>
//     /// <param name="lightLevel">A float value to be treated as a percentage.</param>
//     /// <param name="cooldown">A float value to that a light level dependent system should set for how frequent of a recalculation in light level it should need.
//     /// To be treated as seconds.</param>
//     /// <param name="clamped">If true, Clamp the light level that will be returned to be between 0 and 1 for 0% to 100%.
//     /// If false, the return value can go beyond 100% if the nearest lights have a high enough energy value</param>
//     /// <param name="forceUpdate">If true, disregard any cooldowns in place and force an update in calculated light value.</param>
//     /// <returns>The illumination level of the entity as a float. Treat this as a percentage.</returns>
//     // public bool TryGetLightLevel(EntityUid uid, [NotNullWhen(true)] out float? lightLevel, float cooldown = DefaultCooldown, bool clamped = true, bool forceUpdate = false)
//     // {
//     //     // To gauge something's light level, we need to assign it a corresponding LightSensitiveComponent if it doesn't already have one
//     //     var comp = EnsureComp<LightSensitiveComponent>(uid);

//     //     // We only want to run this update if enough time has passed or it's REALLY important
//     //     // that a component/system operate on precise or frequent updates.
//     //     // For example: If a non player entity like a plant takes damage from being in the dark, that doesn't
//     //     // really need updates every single tick.
//     //     if (forceUpdate || comp.NextUpdate < _gameTiming.CurTime)
//     //     {
//     //         ProcessNearbyLights(uid, comp, Transform(uid));
//     //         comp.NextUpdate = _gameTiming.CurTime + TimeSpan.FromSeconds(cooldown);
//     //     }

//     //     lightLevel = clamped ? Math.Clamp(comp.LightLevel, 0f, 1f) : comp.LightLevel;
//     //     return true;
//     // }

    
// }
