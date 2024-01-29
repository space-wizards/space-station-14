using System.Linq;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Prototypes;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Anomaly;
public sealed partial class AnomalySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    [ValidatePrototypeId<WeightedRandomPrototype>]
    const string WeightListProto = "AnomalyBehaviorList";

    private void InitializeBehavior()
    {

    }

    private string GetRandomBehavior()
    {
        var weightList = _prototype.Index<WeightedRandomPrototype>(WeightListProto);
        return weightList.Pick(_random);
    }

    private void SetBehavior(Entity<AnomalyComponent> anomaly, ProtoId<AnomalyBehaviorPrototype> behaviorProto)
    {
        if (anomaly.Comp.CurrentBehavior == behaviorProto)
            return;

        if (anomaly.Comp.CurrentBehavior != null)
            RemoveBehavior(anomaly);

        anomaly.Comp.CurrentBehavior = behaviorProto;

        var behavior = _prototype.Index(behaviorProto);

        foreach (var (name, entry) in behavior.Components)
        {
            var reg = _componentFactory.GetRegistration(name);

            if (EntityManager.HasComponent(anomaly, reg.Type))
            {
                EntityManager.RemoveComponent(anomaly, reg.Type);
            }

            var comp = (Component) _componentFactory.GetComponent(reg);
            comp.Owner = anomaly;

            var temp = (object) comp;
            _serialization.CopyTo(entry.Component, ref temp);
            EntityManager.RemoveComponent(anomaly, temp!.GetType());
            EntityManager.AddComponent(anomaly, (Component) temp!);
        }
    }

    private void RemoveBehavior(Entity<AnomalyComponent> anomaly)
    {
        if (anomaly.Comp.CurrentBehavior == null)
            return;

        var behavior = _prototype.Index(anomaly.Comp.CurrentBehavior.Value);

        var entityPrototype = MetaData(anomaly).EntityPrototype;
        var toRemove = behavior.Components.Keys.ToList();

        foreach (var name in toRemove)
        {
            // if the entity prototype contained the component originally
            if (entityPrototype?.Components.TryGetComponent(name, out var entry) ?? false)
            {
                var comp = (Component) _componentFactory.GetComponent(name);
                comp.Owner = anomaly;
                var temp = (object) comp;
                _serialization.CopyTo(entry, ref temp);
                EntityManager.RemoveComponent(anomaly, temp!.GetType());
                EntityManager.AddComponent(anomaly, (Component) temp);
                continue;
            }

            EntityManager.RemoveComponentDeferred(anomaly, _componentFactory.GetRegistration(name).Type);
        }
    }
}
