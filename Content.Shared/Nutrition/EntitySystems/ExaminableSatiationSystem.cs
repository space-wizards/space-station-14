using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Makes descriptions specified in <see cref="ExaminableSatiationComponent"/> show up in response to
/// <see cref="ExaminedEvent"/>s.
/// </summary>
/// <seealso cref="ExaminableSatiationComponent"/>
public sealed partial class ExaminableSatiationSystem : BaseSatiationEffectSystem<ExaminableSatiationComponent, LocId?>
{
    protected override Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<LocId?>> GetThresholds(
        ExaminableSatiationComponent comp
    ) => comp.Satiations;

    protected override LocId? DefaultValue() => null;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<ExaminableSatiationComponent> entity, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(entity, EntityManager);
        foreach (var (_, thresholds) in entity.Comp.Satiations)
        {
            if (thresholds.Current is not { } loc)
                continue;

            args.PushMarkup(Loc.GetString(loc, ("entity", identity)));
        }
    }
}
