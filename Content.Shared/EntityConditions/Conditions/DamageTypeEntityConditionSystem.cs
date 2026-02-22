using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;

/// <summary>
/// Returns true if this entity can take damage and if its damage of a given damage type is within a specified minimum and maximum.
/// </summary>
/// <inheritdoc cref="EntityConditionSystem{T, TCondition}"/>
public sealed partial class DamageTypeEntityConditionSystem : EntityConditionSystem<DamageableComponent, DamageTypeCondition>
{
    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<DamageTypeCondition> args)
    {
        var value = entity.Comp.Damage.DamageDict.GetValueOrDefault(args.Condition.DamageType);
        args.Result = value >= args.Condition.Min && value <= args.Condition.Max;
    }
}

/// <inheritdoc cref="EntityCondition"/>
public sealed partial class DamageTypeCondition : EntityConditionBase<DamageTypeCondition>
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> DamageType;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-type-damage",
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()),
            ("min", Min.Float()),
            ("type", prototype.Index(DamageType).LocalizedName));
}
