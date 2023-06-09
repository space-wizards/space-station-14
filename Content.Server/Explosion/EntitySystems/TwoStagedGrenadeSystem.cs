using Content.Server.Explosion.Components;
using Robust.Shared.Timing;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Audio;
using Content.Server.Audio;

namespace Content.Server.Explosion.EntitySystems;

public sealed class TwoStagedGrenadeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;

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
        component.TimeOfNextTrigger = _timing.CurTime + TimeSpan.FromSeconds(component.ExplosionDelay);
        component.TimeOfComponentsAddition = _timing.CurTime + TimeSpan.FromSeconds(component.AddComponentsOffset);
    }

    public void LoadComponents(EntityUid uid, TwoStagedGrenadeComponent component)
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        foreach (var (name, entry) in component.SecondStageComponents)
        {
            var i = (Component) factory.GetComponent(name);
            var temp = (object) i;

            if (_entityManager.TryGetComponent(uid, i.GetType(), out var c))
                _entityManager.RemoveComponent(uid, c);

            i.Owner = uid;
            _serializationManager.CopyTo(entry.Component, ref temp);
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
            if (!component.IsSecondStageActionsBegan && component.TimeOfComponentsAddition <= _timing.CurTime)
            {
                component.IsSecondStageActionsBegan = true;
                LoadComponents(uid, component);
                if (component.IsUsingAmbience && TryComp<AmbientSoundComponent>(uid, out var ambientplayer))
                    _ambient.SetAmbience(uid, true, ambientplayer);
            }
            if (!component.IsSecondStageEnded && component.TimeOfNextTrigger <= _timing.CurTime)
            {
                component.IsSecondStageEnded = true;
                _triggerSystem.Trigger(uid);
            }
        }
    }
}
