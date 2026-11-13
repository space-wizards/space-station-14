using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions.Botany;

/// <summary>
/// Runs conditions on a plant's tray.
/// </summary>
public sealed partial class PlantTrayCondition : EntityConditionBase<PlantTrayCondition>
{
    /// <summary>
    /// Conditions evaluated on the tray.
    /// </summary>
    [DataField]
    public EntityCondition[]? Conditions;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}

/// <summary>
/// Executes entity conditions on the target tray.
/// </summary>
public sealed partial class PlantTrayEntityConditionSystem : EntitySystem
{
    [Dependency] private PlantSystem _plant = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;

    [SubscribeLocalEvent]
    private void OnTray(Entity<TransformComponent> ent, ref EntityConditionEvent<PlantTrayCondition> args)
    {
        if (!TryGetTray(ent.Owner, out var tray))
            return;

        var condition = args.Condition;
        args.Result = _conditions.TryConditions(tray.Owner, condition.Conditions, args.SourceEnt);
    }

    private bool TryGetTray(EntityUid uid, out Entity<PlantTrayComponent> tray)
    {
        if (TryComp<PlantTrayComponent>(uid, out var component))
        {
            tray = (uid, component);
            return true;
        }

        return _plant.TryGetTray(uid, out tray);
    }
}
