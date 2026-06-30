using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// Makes descriptions specified in <see cref="ExaminableSatiationComponent"/> show up in response to
/// <see cref="ExaminedEvent"/>s.
/// </summary>
/// <seealso cref="ExaminableSatiationComponent"/>
public sealed partial class ExaminableSatiationSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;

    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExaminableSatiationComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ExaminableSatiationComponent> entity, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(entity, EntityManager);
        _satiationQuery.TryComp(entity, out var satiationComp);

        foreach (var (satType, exSatProto) in entity.Comp.Satiations)
        {
            if (!ProtoMan.TryIndex(exSatProto, out var exSatiation))
                continue;

            if (satiationComp is null ||
                !_satiation.TryGetValueByThreshold((entity.Owner, satiationComp),
                    satType,
                    exSatiation.Descriptions,
                    out var descriptionLocId))
            {
                descriptionLocId = exSatiation.NotApplicable;
            }

            args.PushMarkup(Loc.GetString(descriptionLocId, ("entity", identity)));
        }
    }
}
