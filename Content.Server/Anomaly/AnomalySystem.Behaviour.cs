using System.Linq;
using Content.Shared.Anomaly.Components;
using Content.Shared.Anomaly.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.Anomaly;
public sealed partial class AnomalySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private List<AnomalyBehaviourPrototype> _behaviourList = new();
    private void InitializeBehaviour()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        //Cache all behaviors into a list at the beginning of the round
        _behaviourList.AddRange(_prototype.EnumeratePrototypes<AnomalyBehaviourPrototype>());
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        _behaviourList.Clear();
        _behaviourList.AddRange(_prototype.EnumeratePrototypes<AnomalyBehaviourPrototype>());
    }

    private string GetRandomBehaviour()
    {
        var totalWeight = _behaviourList.Sum(x => x.Weight);
        var randomValue = _random.NextFloat(totalWeight);

        foreach (var b in _behaviourList)
        {
            if (randomValue < b.Weight)
            {
                return _random.Pick(_behaviourList).ID;
            }

            randomValue -= b.Weight;
        }
        // Shouldn't happen
        throw new InvalidOperationException($"Invalid weighted variantize anomaly behaviour pick!");
    }

    private void SetBehaviour(Entity<AnomalyComponent> anomaly, ProtoId<AnomalyBehaviourPrototype> behaviourProto)
    {
        if (anomaly.Comp.CurrentBehaviour == behaviourProto)
            return;

        if (anomaly.Comp.CurrentBehaviour != null)
            RemoveBehaviour(anomaly);

        anomaly.Comp.CurrentBehaviour = behaviourProto;

        var behaviour = _prototype.Index(behaviourProto);

        foreach (var (name, entry) in behaviour.Components)
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

    private void RemoveBehaviour(Entity<AnomalyComponent> anomaly)
    {
        if (anomaly.Comp.CurrentBehaviour == null)
            return;

        var behaviour = _prototype.Index(anomaly.Comp.CurrentBehaviour.Value);

        var entityPrototype = MetaData(anomaly).EntityPrototype;
        var toRemove = behaviour.Components.Keys.ToList();

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
