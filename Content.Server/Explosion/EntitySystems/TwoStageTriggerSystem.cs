using Robust.Shared.Timing;
using Robust.Shared.Serialization.Manager;
using Content.Server.Explosion.Components.OnTrigger;

namespace Content.Server.Explosion.EntitySystems;

public sealed class TwoStageTriggerSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TwoStageTriggerComponent, EntityUnpausedEvent>(OnTriggerUnpaused);
        SubscribeLocalEvent<TwoStageTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTriggerUnpaused(EntityUid uid, TwoStageTriggerComponent component, ref EntityUnpausedEvent args)
    {
        if (component.NextTriggerTime != null)
            component.NextTriggerTime = component.NextTriggerTime.Value + args.PausedTime;
    }

    private void OnTrigger(EntityUid uid, TwoStageTriggerComponent component, TriggerEvent args)
    {
        if (component.Triggered)
            return;

        component.Triggered = true;
        component.NextTriggerTime = _timing.CurTime + component.TriggerDelay;
    }

    private void LoadComponents(EntityUid uid, TwoStageTriggerComponent component)
    {
        foreach (var (name, entry) in component.SecondStageComponents)
        {
            var comp = (Component) _factory.GetComponent(name);
            var temp = (object) comp;

            if (EntityManager.TryGetComponent(uid, entry.Component.GetType(), out var c))
                RemComp(uid, c);

            comp.Owner = uid;
            _serializationManager.CopyTo(entry.Component, ref temp);
            EntityManager.AddComponent(uid, comp);
        }
        component.ComponentsIsLoaded = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<TwoStageTriggerComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (!component.Triggered)
                continue;

            if (!component.ComponentsIsLoaded)
                LoadComponents(uid, component);

            if (_timing.CurTime < component.NextTriggerTime)
                continue;

            component.NextTriggerTime = null;
            _triggerSystem.Trigger(uid);
        }
    }
}
