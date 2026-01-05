using Content.Shared.Botany.Components;
using Content.Shared.Botany.Events;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Botany.Items.Systems;

/// <summary>
/// System for taking a sample of a plant.
/// </summary>
public sealed class BotanySampleTakerSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BotanySampleTakerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantComponent, PlantSampleAttemptEvent>(OnPlantSampleAttempt);
    }

    private void OnAfterInteract(Entity<BotanySampleTakerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled || !args.CanReach || !HasComp<PlantComponent>(args.Target))
            return;

        var ev = new PlantSampleAttemptEvent(ent, args.User);
        RaiseLocalEvent(args.Target.Value, ref ev);

        args.Handled = true;
    }

    private void OnPlantSampleAttempt(Entity<PlantComponent> ent, ref PlantSampleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<PlantHolderComponent>(ent.Owner, out var holder)
            || !TryComp<PlantDataComponent>(ent.Owner, out var plantData)
            || !TryComp<PlantHarvestComponent>(ent.Owner, out var harvest))
            return;

        // Prevent early sampling.
        var growthStage = _plant.GetGrowthStageValue(ent.AsNullable());
        if (growthStage <= args.Sample.Comp.MinSampleStage)
        {
            _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-early-sample-message"), args.User);
            return;
        }

        // TODO: Replace with RandomPredicted once the engine PR is merged
        var seed = SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent).Id);
        var rand = new System.Random(seed);

        // Damage the plant.
        _plantHolder.AdjustsHealth(ent.Owner, -rand.Next(args.Sample.Comp.MinSampleDamage, args.Sample.Comp.MaxSampleDamage));

        // Produce a seed packet snapshot.
        float? healthOverride = harvest.ReadyForHarvest ? null : holder.Health;
        _botany.SpawnSeedPacketFromPlant(ent.Owner, Transform(args.User).Coordinates, args.User, healthOverride);

        var displayName = Loc.GetString(plantData.DisplayName);
        _popup.PopupPredictedCursor(Loc.GetString("plant-holder-component-take-sample-message", ("seedName", displayName)), args.User);

        if (rand.Prob(args.Sample.Comp.SampleProbability))
            _plantTraits.AddTrait(ent.Owner, new TraitSampled());
    }
}
