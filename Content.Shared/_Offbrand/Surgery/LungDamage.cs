using Content.Shared._Offbrand.Wounds;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class LungDamage : IGraphCondition
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<LungDamageComponent>(uid, out var lungDamage))
            return false;

        return lungDamage.Damage >= Min && lungDamage.Damage <= Max;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<LungDamageComponent>(args.Examined, out var lungDamage))
            return false;

        if (lungDamage.Damage >= Min && lungDamage.Damage <= Max)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-lung-damage-range", ("min", Min.Float()), ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float())));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-lung-damage-range",
            Arguments =
                [ ("min", Min.Float()), ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()) ],
        };
    }
}
