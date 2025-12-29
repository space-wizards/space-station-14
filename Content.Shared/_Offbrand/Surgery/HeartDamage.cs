using Content.Shared._Offbrand.Wounds;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class HeartDamage : IGraphCondition
{
    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<HeartrateComponent>(uid, out var heartrate))
            return false;

        return heartrate.Damage >= Min && heartrate.Damage <= Max;
    }

    public bool DoExamine(ExaminedEvent args)
    {
        if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<HeartrateComponent>(args.Examined, out var heartrate))
            return false;

        if (heartrate.Damage >= Min && heartrate.Damage <= Max)
            return false;

        args.PushMarkup(Loc.GetString("construction-examine-heart-damage-range", ("min", Min.Float()), ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float())));
        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-heart-damage-range",
            Arguments =
                [ ("min", Min.Float()), ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()) ],
        };
    }
}
