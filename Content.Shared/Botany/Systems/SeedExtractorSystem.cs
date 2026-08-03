using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Systems;

public sealed partial class SeedExtractorSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<SeedExtractorComponent> ent, ref InteractUsingEvent args)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        if (!TryComp<ProduceComponent>(args.Used, out var produce))
            return;

        if (produce.PlantProtoId == null)
            return;

        EntityUid? snapshot = null;
        if (produce.PlantData != null)
            snapshot = produce.PlantData;

        if (_botany.TryGetPlantComponent<PlantTraitSeedlessComponent>(snapshot, produce.PlantProtoId, out _))
        {
            _popup.PopupCursor(Loc.GetString("seed-extractor-component-no-seeds", ("name", args.Used)),
                args.User,
                PopupType.MediumCaution);
            return;
        }

        _popup.PopupCursor(Loc.GetString("seed-extractor-component-interact-message", ("name", args.Used)),
            args.User,
            PopupType.Medium);

        PredictedQueueDel(args.Used);
        args.Handled = true;


        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var amount = random.NextFloat(ent.Comp.BaseSeeds.Min, ent.Comp.BaseSeeds.Max + 1);
        var coords = Transform(ent).Coordinates;

        for (var i = 0; i < amount; i++)
        {
            if (_botany.TryGetPlantComponent<PlantDataComponent>(snapshot, produce.PlantProtoId, out var plantData))
                _botany.SpawnSeedPacket(plantData, produce.PlantProtoId.Value, snapshot, coords, args.User);
        }
    }
}
