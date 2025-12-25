using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

public sealed class SeedExtractorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly BotanySystem _botany = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedExtractorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, SeedExtractorComponent seedExtractor, InteractUsingEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!TryComp<ProduceComponent>(args.Used, out var produce))
            return;

        if (produce.PlantProtoId == null)
            return;

        ComponentRegistry? snapshot = null;
        if (produce.PlantData != null)
            snapshot = produce.PlantData;

        if (_botany.TryGetPlantComponent<PlantTraitsComponent>(snapshot, produce.PlantProtoId, out var traits) && traits.Seedless)
        {
            _popup.PopupCursor(Loc.GetString("seed-extractor-component-no-seeds", ("name", args.Used)),
                args.User, PopupType.MediumCaution);
            return;
        }

        _popup.PopupCursor(Loc.GetString("seed-extractor-component-interact-message", ("name", args.Used)),
            args.User, PopupType.Medium);

        QueueDel(args.Used);
        args.Handled = true;

        var amount = _random.Next(seedExtractor.BaseMinSeeds, seedExtractor.BaseMaxSeeds + 1);
        var coords = Transform(uid).Coordinates;

        for (var i = 0; i < amount; i++)
        {
            _botany.SpawnSeedPacketFromSnapshot(snapshot, produce.PlantProtoId.Value, coords, args.User);
        }
    }
}
