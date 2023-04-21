using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Construction;
using Content.Server.Nutrition.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.Dataset;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
/// This handles logic and interactions relating to <see cref="FatExtractorComponent"/>
/// </summary>
public sealed class FatExtractorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FatExtractorComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<FatExtractorComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<FatExtractorComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<FatExtractorComponent, GotEmaggedEvent>(OnGotEmagged);
        SubscribeLocalEvent<FatExtractorComponent, StorageAfterCloseEvent>(OnClosed);
        SubscribeLocalEvent<FatExtractorComponent, StorageAfterOpenEvent>(OnOpen);

        SubscribeLocalEvent<ActiveFatExtractorComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnRefreshParts(EntityUid uid, FatExtractorComponent component, RefreshPartsEvent args)
    {
        var rating = args.PartRatings[component.MachinePartNutritionRate] - 1;
        component.NutritionPerSecond = component.BaseNutritionPerSecond + (int) (component.PartRatingRateMultiplier * rating);
    }

    private void OnUpgradeExamine(EntityUid uid, FatExtractorComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("fat-extractor-component-rate", (float) component.NutritionPerSecond / component.BaseNutritionPerSecond);
    }

    private void OnUnpaused(EntityUid uid, FatExtractorComponent component, ref EntityUnpausedEvent args)
    {
        component.NextUpdate += args.PausedTime;
    }

    private void OnGotEmagged(EntityUid uid, FatExtractorComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
        args.Repeatable = false;
    }

    private void OnClosed(EntityUid uid, FatExtractorComponent component, ref StorageAfterCloseEvent args)
    {
        StartProcessing(uid, component);
    }

    private void OnOpen(EntityUid uid, FatExtractorComponent component, ref StorageAfterOpenEvent args)
    {
        StopProcessing(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, ActiveFatExtractorComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            StopProcessing(uid, active: component);
    }

    public void StartProcessing(EntityUid uid, FatExtractorComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return;

        if (HasComp<ActiveFatExtractorComponent>(uid))
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!TryGetValidOccupant(uid, out _, component, storage))
            return;

        EnsureComp<ActiveFatExtractorComponent>(uid);
        _appearance.SetData(uid, FatExtractorVisuals.Processing, true);
        component.Stream = _audio.PlayPvs(component.ProcessSound, uid);
        component.NextUpdate = _timing.CurTime + component.UpdateTime;
    }

    public void StopProcessing(EntityUid uid, FatExtractorComponent? component = null, ActiveFatExtractorComponent? active = null)
    {
        if (!Resolve(uid, ref component, ref active, false))
            return;

        RemComp(uid, active);
        _appearance.SetData(uid, FatExtractorVisuals.Processing, false);
        component.Stream?.Stop();
    }

    public bool TryGetValidOccupant(EntityUid uid, [NotNullWhen(true)] out EntityUid? occupant, FatExtractorComponent? component = null, EntityStorageComponent? storage = null)
    {
        occupant = null;
        if (!Resolve(uid, ref component, ref storage))
            return false;

        occupant = storage.Contents.ContainedEntities.FirstOrDefault();

        if (!TryComp<HungerComponent>(occupant, out var hunger))
            return false;

        if (hunger.CurrentHunger < component.NutritionPerSecond)
            return false;

        if (hunger.CurrentThreshold < component.MinHungerThreshold && !HasComp<EmaggedComponent>(uid))
            return false;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveFatExtractorComponent, FatExtractorComponent, EntityStorageComponent>();
        while (query.MoveNext(out var uid, out var active, out var fat, out var storage))
        {
            if (_timing.CurTime < fat.NextUpdate)
                continue;
            fat.NextUpdate = _timing.CurTime + fat.UpdateTime;

            if (!TryGetValidOccupant(uid, out var occupant, fat, storage))
            {
                StopProcessing(uid, fat, active);
                continue;
            }

            _hunger.ModifyHunger(occupant.Value, -fat.NutritionPerSecond);
            fat.NutrientAccumulator += fat.NutritionPerSecond;
            if (fat.NutrientAccumulator >= fat.NutrientPerMeat)
            {
                fat.NutrientAccumulator -= fat.NutrientPerMeat;
                Spawn(fat.MeatPrototype, Transform(uid).Coordinates);
            }
        }

        // we have to add an extra query here in case you gain hunger while inside.
        // sorry for activecomp betrayal.
        var checkQuery = EntityQueryEnumerator<FatExtractorComponent, EntityStorageComponent>();
        while (checkQuery.MoveNext(out var uid, out var fat, out var storage))
        {
            StartProcessing(uid, fat, storage);
        }
    }
}
