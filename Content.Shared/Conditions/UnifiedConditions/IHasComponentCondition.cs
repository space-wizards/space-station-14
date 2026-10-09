using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Conditions.UnifiedConditions;

public interface IHasComponentsCondition : ICondition
{
    /// <summary>
    /// Name of the components we look for
    /// </summary>
    string[] Components { get; }
}


public  sealed partial class HasComponentCondition : ConditionEvaluatorSystem<IHasComponentsCondition>
{

    [Dependency] private IEntityManager _entityManager = default!;

    [Dependency] private IComponentFactory _componentFactory = default!;

    public override float Evaluate(IHasComponentsCondition condition, EntityUid entityUid, EntityUid? sourceEntity = null)
    {
        if (condition.Components.Length == 0)
            return 0;

        float value = 0;
        foreach (var component in condition.Components)
        {
            if(!_componentFactory.TryGetRegistration(component, out var registration))
                continue;
            if (_entityManager.HasComponent(entityUid, registration))
                value++;
        }

        return value/condition.Components.Length;

    }
}
