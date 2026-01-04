
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// System for taking a sample of a plant.
/// </summary>
public sealed class BotanySampleTakerSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PlantSystem _plant = default!;
    [Dependency] private readonly PlantTraitsSystem _plantTraits = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
            _popup.PopupCursor(Loc.GetString("plant-holder-component-early-sample-message"), args.User);
            return;
        }

        // Damage the plant.
        _plantHolder.AdjustsHealth(ent.Owner, -_random.Next(args.Sample.Comp.MinSampleDamage, args.Sample.Comp.MaxSampleDamage));

        // Produce a seed packet snapshot.
        float? healthOverride = harvest.ReadyForHarvest ? null : holder.Health;
        _botany.SpawnSeedPacketFromPlant(ent.Owner, Transform(args.User).Coordinates, args.User, healthOverride);

        var displayName = Loc.GetString(plantData.DisplayName);
        _popup.PopupCursor(Loc.GetString("plant-holder-component-take-sample-message", ("seedName", displayName)), args.User);

        if (_random.Prob(args.Sample.Comp.SampleProbability))
            _plantTraits.AddTrait(ent.Owner, new TraitSampled());
    }
}

[ByRefEvent]
public sealed class PlantSampleAttemptEvent(Entity<BotanySampleTakerComponent> sample, EntityUid user) : CancellableEntityEventArgs
{
    public Entity<BotanySampleTakerComponent> Sample { get; } = sample;
    public EntityUid User { get; } = user;
}
