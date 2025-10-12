using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class TotalGroupDamage : EntityEffectCondition
{
    [DataField(required: true)]
    public ProtoId<DamageGroupPrototype> Group;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>();
        var group = prototype.Index(Group);

        if (!args.EntityManager.TryGetComponent<DamageableComponent>(args.TargetEntity, out var damage))
            return false;

        var total = FixedPoint2.Zero;
        damage.Damage.TryGetDamageInGroup(group, out total);
        return total >= Min && total <= Max;
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-guidebook-total-group-damage",
            ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
            ("min", Min.Float()),
            ("name", prototype.Index(Group).LocalizedName));
    }
}
