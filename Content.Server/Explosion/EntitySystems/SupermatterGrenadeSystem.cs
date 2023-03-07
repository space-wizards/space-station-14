using Content.Server.Explosion.Components;
using Content.Shared.Singularity.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Content.Shared.Audio;
using Content.Server.Audio;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Content.Server.Singularity.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed class SupermatterGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly PointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterGrenadeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SupermatterGrenadeComponent, TriggerEvent>(OnExplode);
    }

    private void OnStartup(EntityUid uid, SupermatterGrenadeComponent component, ComponentStartup args)
    {
        if(TryComp<SingularityDistortionComponent>(uid, out var singulo))
            component.Distortion = singulo;
        if (TryComp<GravityWellComponent>(uid, out var well))
            component.GravityWell = well;
    }
    private void OnExplode(EntityUid uid, SupermatterGrenadeComponent component, TriggerEvent args)
    {
        if (component.IsExploded)
        {
            if (component.IsGravityPulling && component.GravityPullEndSound != null)
                _audio.PlayPvs(component.GravityPullEndSound, uid,
                    AudioParams.Default.WithVolume(component.GravityPullEndSoundVolume));
            return;
        }
        _container.TryRemoveFromContainer(uid, true);
        if (component.AnchorOnGravityPull)
            _transformSystem.AnchorEntity(uid, Transform(uid));
        var param = AudioParams.Default;
        if (component.GravityPullStartSound != null)
            _audio.PlayPvs(component.GravityPullStartSound, uid,
                AudioParams.Default.WithVolume(component.GravityPullStartSoundVolume));
        if (HasComp<PointLightComponent>(uid))
            _pointLightSystem.SetEnabled(uid, true);
        if (component.Distortion != null)
            component.Distortion.Intensity = component.DistortionIntensity;
        if (component.GravityWell != null)
            component.GravityWell.BaseRadialAcceleration = component.BaseRadialAcceleration;

        component.IsGravityPulling = true;
        component.ExplosionWillOccurIn =
            _timing.CurTime + TimeSpan.FromSeconds(component.TimeTillExplosion);
        component.GravityPullWillOccurIn =
            _timing.CurTime + TimeSpan.FromSeconds(component.GravityPullLoopSoundOffset);
        if (!HasComp<ExplodeOnTriggerComponent>(uid) && !HasComp<DeleteOnTriggerComponent>(uid))
        {
            if (component.ExplodeAfterGravityPull)
                _entityManager.AddComponent<ExplodeOnTriggerComponent>(uid);
            else
                _entityManager.AddComponent<DeleteOnTriggerComponent>(uid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<SupermatterGrenadeComponent>();
        while (enumerator.MoveNext(out var component))
        {
            if (!component.IsGravityPulling)
                continue;

            #pragma warning disable
            var uid = component.Owner;
            #pragma warning enable
            if (!component.IsGravitySoundBegan && component.GravityPullWillOccurIn <= _timing.CurTime)
            {
                component.IsGravitySoundBegan = true;
                if (TryComp<AmbientSoundComponent>(uid, out var ambience))
                    _ambient.SetAmbience(uid, true, ambience);
            }
            if (!component.IsExploded && component.ExplosionWillOccurIn <= _timing.CurTime)
            {
                component.IsExploded = true;
                _triggerSystem.Trigger(uid);
            }
        }
    }
}
