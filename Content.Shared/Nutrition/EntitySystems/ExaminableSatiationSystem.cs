using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Makes descriptions specified in <see cref="ExaminableSatiationComponent"/> show up in response to
/// <see cref="ExaminedEvent"/>s.
/// </summary>
/// <seealso cref="ExaminableSatiationComponent"/>
public sealed partial class ExaminableSatiationSystem : BaseSatiationEffectSystem<ExaminableSatiationComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExaminableSatiationComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ExaminableSatiationComponent> entity, ref ExaminedEvent args)
    {
        if (!SatiationQuery.TryComp(entity, out var satiationComp))
            return;
        var satiation = new Entity<SatiationComponent>(entity, satiationComp);
        var identity = Identity.Entity(entity, EntityManager);

        foreach (var (satType, thresholds) in entity.Comp.Satiations)
        {
            if (!SatiationSystem.TryGetValueByThreshold(
                    satiation,
                    satType,
                    thresholds,
                    out var descriptionLocId
                ) || descriptionLocId == null)
                continue;

            args.PushMarkup(Loc.GetString(descriptionLocId, ("entity", identity)));
        }
    }
}
