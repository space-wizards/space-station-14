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
public sealed class ExaminableSatiationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SatiationSystem _satiation = default!;

    private EntityQuery<SatiationComponent> _satiationQuery;

    public override void Initialize()
    {
        _satiationQuery = GetEntityQuery<SatiationComponent>();

        SubscribeLocalEvent<ExaminableSatiationComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ExaminableSatiationComponent> entity, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(entity, EntityManager);
        _satiationQuery.TryComp(entity, out var satiationComp);

        foreach (var (satType, exSatProto) in entity.Comp.Satiations)
        {
            if (!_prototype.TryIndex(exSatProto, out var exSatiation))
                continue;

            var thresholdOrNull = satiationComp is not null
                ? _satiation.GetThresholdOrNull((entity.Owner, satiationComp), satType)
                : null;
            var msg = Loc.GetString(exSatiation.GetDescriptionOrDefault(thresholdOrNull),
                ("entity", identity));
            args.PushMarkup(msg);
        }
    }
}
