using Content.Shared._Offbrand.Organs;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.EntityConditions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class OrganDamageCondition : EntityConditionBase<OrganDamageCondition>
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-heart-damage",
            ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
            ("min", Min.Float()));
    }
}

public sealed class OrganDamageEntityConditionSystem : EntityConditionSystem<BodyComponent, OrganDamageCondition>
{
    [Dependency] private readonly BodySystem _body = default!;

    protected override void Condition(Entity<BodyComponent> ent, ref EntityConditionEvent<OrganDamageCondition> args)
    {
        if (!_body.TryGetOrgansWithCategoryAndComponent<DamageableOrganComponent>(ent.AsNullable(),
                out var organs,
                args.Condition.Category))
        {
            args.Result = false;
            return;
        }

        args.Result = organs[0].Comp2.Damage >= args.Condition.Min && organs[0].Comp2.Damage <= args.Condition.Max;
    }
}
