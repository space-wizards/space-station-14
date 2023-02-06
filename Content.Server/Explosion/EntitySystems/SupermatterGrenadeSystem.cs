using Content.Server.Explosion.Components;
using Content.Shared.Singularity.Components;
using Content.Server.Singularity.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Content.Shared.Audio;
using Content.Server.Audio;
using Robust.Shared.Containers;

namespace Content.Server.Explosion.EntitySystems;

public sealed class SupermatterGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterGrenadeComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SupermatterGrenadeComponent, TriggerEvent>(OnExplode);
    }

    private void OnInit(EntityUid uid, SupermatterGrenadeComponent component, ComponentInit args)
    {
        var distortion = Comp<SingularityDistortionComponent>(uid);
        component.DistortionIntensity = distortion.Intensity;
        distortion.Intensity = 0;

        var gravity = Comp<GravityWellComponent>(uid);
        component.BaseRadialAcceleration = gravity.BaseRadialAcceleration;
        gravity.BaseRadialAcceleration = 0;
    }

    private void OnExplode(EntityUid uid, SupermatterGrenadeComponent component, TriggerEvent args)
    {
        if (component.IsExploded)
        {
            if (component.IsGravityPulling)
            {
                if (component.GravityPullEndSound != null)
                    _audio.PlayPvs(component.GravityPullEndSound, uid,
                        AudioParams.Default.WithVolume(component.GravityPullEndSoundVolume));
            }
            return;
        }
        _container.TryRemoveFromContainer(uid, true);
        if (component.AnchorOnGravityPull)
            _transformSystem.AnchorEntity(Transform(uid));

        var param = AudioParams.Default;
        if (component.GravityPullStartSound != null)
            _audio.PlayPvs(component.GravityPullStartSound, uid,
                AudioParams.Default.WithVolume(component.GravityPullStartSoundVolume));

        component.Distortion = Comp<SingularityDistortionComponent>(uid);
        component.Distortion.Intensity = component.DistortionIntensity;
        component.GravityWell = Comp<GravityWellComponent>(uid);
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
        foreach (var component in EntityQuery<SupermatterGrenadeComponent>())
        {
            var uid = component.Owner;
            if (!component.IsGravityPulling || component.Distortion == null)
                return;
            if (!component.IsGravitySoundBegan && component.GravityPullWillOccurIn <= _timing.CurTime)
            {
                component.IsGravitySoundBegan = true;
                var ambience = Comp<AmbientSoundComponent>(uid);
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
