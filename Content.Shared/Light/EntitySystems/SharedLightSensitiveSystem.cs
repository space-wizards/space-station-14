using System.Diagnostics.CodeAnalysis;
using Content.Shared.Light.Components;
using Robust.Shared.ComponentTrees;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Light;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Light.EntitySystems;

/// <summary>
///     Handles the calculations of light sensitivity for entities with the <see cref="LightSensitiveComponent"/>.
///     Due to the potential performance impact of calculating the illumination of an unspecified number of entities of varying importance and tick rates,
///     this system will not be enabled by default and even when enabled will not execute until entites exist with the corresponding Component and
///     specifically request updates.
///     I did my best to optimize this but use and implement cautiously.
/// </summary>
public sealed class SharedLightSensitiveSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly LightLevelSystem _level = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightSensitiveComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(EntityUid uid, LightSensitiveComponent component, ComponentStartup args)
    {
        UpdateEntityIllumination(uid, component);
    }

    public bool ResolveComp(EntityUid uid, out LightSensitiveComponent component)
    {
        component = EnsureComp<LightSensitiveComponent>(uid);

        return component != null;
    }

    private void SetIllumination(EntityUid uid, float value, LightSensitiveComponent comp)
    {

        var ev = new EntityLightUpdateEvent(uid, value);
        RaiseLocalEvent(uid, ev);

        if (MathHelper.CloseTo(ev.LightLevel, comp.LightLevel))
            return;

        comp.LightLevel = value;
        Dirty(uid, comp);
    }

    // public bool TryGetEntityIllumination(EntityUid uid, [NotNullWhen(true)] out float illumination, ILightLevelDependent? lightDep = null)
    // {

    // }

    public float GetEntityIllumination(EntityUid uid, ILightLevelDependent lightDep, bool forceUpdate = false)
    {
        if (!ResolveComp(uid, out var lightComp))
            return 0f;

        if (forceUpdate || lightComp.LastUpdate + lightDep.UpdateCooldown < _gameTiming.CurTime)
        {
            UpdateEntityIllumination(uid, lightComp, lightDep);
        }

        return lightComp.LightLevel;
    }

    /// <summary>
    ///     Gets all PointLightComponents near an entity by querying the light tree with the entity's position and bounding box.
    ///     Then checks for occlusion between each light and entity by raycasting against the occluderTree, and if the entity is in range of its radius.
    ///     Lights that are in range and unoccluded then have their light level calculated by multiplying their energy by a modified attenuation formula.
    ///     Only the highest light level is kept because I don't want to mess with adding or multiplying light values together.
    /// </summary>
    /// <remarks>
    ///     This method is probably going to be very performance inefficient, so try not to use it too often. We store recent light level calculations
    ///     in the LightSensitiveComponent, so it's not necessary to calculate them every single tick.
    /// </remarks>
    /// <param name="uid">Entity UID to check.</param>
    /// <param name="component">The LightSensitiveComponent of the entity</param>
    /// <param name="entityXform">The TransformComponent of the entity</param>
    private void UpdateEntityIllumination(EntityUid uid, LightSensitiveComponent component, ILightLevelDependent? lightDep = null)
    {
        var pos = _transform.GetMapCoordinates(uid);
        var illumination = _level.CalculateLightLevel(pos);

        SetIllumination(uid, illumination, component);
        if (lightDep == null)
            return;

        component.LastUpdate = _gameTiming.CurTime;
    }

    private void UpdateEntityIllumination(EntityUid uid, ILightLevelDependent? lightDep = null)
    {
        var comp = EnsureComp<LightSensitiveComponent>(uid);
        UpdateEntityIllumination(uid, comp);
    }

}


/// <summary>
/// 
/// </summary>
public sealed class EntityLightUpdateEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly float LightLevel;

    public EntityLightUpdateEvent(EntityUid source, float lightLevel)
    {
        Source = source;
        LightLevel = lightLevel;
    }
}
