using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

public sealed class SeedExtractorSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedExtractorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<SeedExtractorComponent> ent, ref InteractUsingEvent args)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        if (!TryComp<ProduceComponent>(args.Used, out var produce))
            return;

        if (produce.PlantProtoId == null)
            return;

        ComponentRegistry? snapshot = null;
        if (produce.PlantData != null)
            snapshot = produce.PlantData;

        if (_botany.TryGetPlantComponent<PlantTraitsComponent>(snapshot, produce.PlantProtoId, out var traits) &&
            _plantTraits.TryGetTrait<TraitSeedless>(traits, out _))
        {
            _popup.PopupPredictedCursor(Loc.GetString("seed-extractor-component-no-seeds", ("name", args.Used)),
                args.User,
                PopupType.MediumCaution);
            return;
        }

        _popup.PopupPredictedCursor(Loc.GetString("seed-extractor-component-interact-message", ("name", args.Used)),
            args.User,
            PopupType.Medium);

        PredictedQueueDel(args.Used);
        args.Handled = true;

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        var amount = rand.Next(ent.Comp.BaseMinSeeds, ent.Comp.BaseMaxSeeds + 1);
        var coords = Transform(ent).Coordinates;

        for (var i = 0; i < amount; i++)
        {
            _botany.SpawnSeedPacketFromSnapshot(snapshot, produce.PlantProtoId.Value, coords, args.User);
        }
    }
}
