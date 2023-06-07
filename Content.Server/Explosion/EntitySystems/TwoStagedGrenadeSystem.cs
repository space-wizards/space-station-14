using Content.Server.Explosion.Components;
using Robust.Shared.Timing;
using Content.Shared.Audio;
using Content.Server.Audio;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Explosion.EntitySystems;

public sealed class TwoStagedGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TwoStagedGrenadeComponent, TriggerEvent>(OnExplode);
    }

    private void OnExplode(EntityUid uid, TwoStagedGrenadeComponent component, TriggerEvent args)
    {
        if (component.IsSecondStageEnded)
            return;

        component.IsSecondStageBegan = true;
        component.TimeOfExplosion = _timing.CurTime + TimeSpan.FromSeconds(component.ExplosionDelay);
        component.AmbienceStartTime = _timing.CurTime + TimeSpan.FromSeconds(component.AmbienceSoundOffset);
    }

    public void LoadComponents(EntityUid uid, TwoStagedGrenadeComponent component)
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        foreach (var (name, entry) in component.SecondStageComponents)
        {
            var i = (Component) factory.GetComponent(name);
            var temp = (object) i;
            _serializationManager.CopyTo(entry.Component, ref temp);

            if(_entityManager.TryGetComponent(uid, i.GetType(), out var c))
                _entityManager.RemoveComponent(uid, c);
            i.Owner = uid;
            _entityManager.AddComponent(uid, (Component)temp!);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<TwoStagedGrenadeComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (!component.IsSecondStageBegan)
                continue;

            if (!component.IsComponentsLoaded)
            {
                component.IsComponentsLoaded = true;
                LoadComponents(uid, component);
            }
            if (!component.IsSecondStageSoundBegan && component.AmbienceStartTime <= _timing.CurTime)
            {
                component.IsSecondStageSoundBegan = true;
                if (TryComp<AmbientSoundComponent>(uid, out var ambientplayer))
                    _ambient.SetAmbience(uid, true, ambientplayer);
            }
            if (!component.IsSecondStageEnded && component.TimeOfExplosion <= _timing.CurTime)
            {
                component.IsSecondStageEnded = true;
                _triggerSystem.Trigger(uid);
            }
        }
    }
}
