using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Nutrition.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
/// This handles logic and interactions relating to <see cref="FatExtractorComponent"/>
/// </summary>
public sealed partial class FatExtractorSystem : EntitySystem
{
    [Dependency] private IEntityTimerManager _timers = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly EntityTimerId ProcessingTimer = new("processing");

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<FatExtractorComponent, GotEmaggedEvent>(OnGotEmagged);
        SubscribeLocalEvent<FatExtractorComponent, StorageAfterCloseEvent>(OnClosed);
        SubscribeLocalEvent<FatExtractorComponent, StorageAfterOpenEvent>(OnOpen);
        SubscribeLocalEvent<FatExtractorComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<FatExtractorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FatExtractorComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnStartup(Entity<FatExtractorComponent> ent, ref ComponentStartup args)
    {
        _timers.SetTimer(ent, ProcessingTimer, ent.Comp.UpdateTime, ent.Comp.UpdateTime);
    }

    private void OnGotEmagged(EntityUid uid, FatExtractorComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(uid, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private void OnClosed(EntityUid uid, FatExtractorComponent component, ref StorageAfterCloseEvent args)
    {
        StartProcessing(uid, component);
    }

    private void OnOpen(EntityUid uid, FatExtractorComponent component, ref StorageAfterOpenEvent args)
    {
        StopProcessing(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, FatExtractorComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            StopProcessing(uid, component);
    }

    public void StartProcessing(EntityUid uid, FatExtractorComponent? component = null, EntityStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref component, ref storage))
            return;

        if (component.Processing)
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!TryGetValidOccupant(uid, out _, component, storage))
            return;

        component.Processing = true;
        _appearance.SetData(uid, FatExtractorVisuals.Processing, true);
        component.Stream = _audio.PlayPvs(component.ProcessSound, uid)?.Entity;
    }

    public void StopProcessing(EntityUid uid, FatExtractorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.Processing)
            return;

        component.Processing = false;
        _appearance.SetData(uid, FatExtractorVisuals.Processing, false);
        component.Stream = _audio.Stop(component.Stream);
    }

    public bool TryGetValidOccupant(EntityUid uid, [NotNullWhen(true)] out EntityUid? occupant, FatExtractorComponent? component = null, EntityStorageComponent? storage = null)
    {
        occupant = null;
        if (!Resolve(uid, ref component, ref storage))
            return false;

        occupant = storage.Contents.ContainedEntities.FirstOrDefault();

        if (!TryComp<HungerComponent>(occupant, out var hunger))
            return false;

        if (_hunger.GetHunger(hunger) < component.NutritionPerSecond)
            return false;

        if (hunger.CurrentThreshold < component.MinHungerThreshold && !_emag.CheckFlag(uid, EmagType.Interaction))
            return false;

        return true;
    }

    private void OnTimer(Entity<FatExtractorComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != ProcessingTimer || !TryComp(ent, out EntityStorageComponent? storage))
            return;

        if (TryGetValidOccupant(ent.Owner, out var occupant, ent.Comp, storage))
        {
            if (!ent.Comp.Processing)
                StartProcessing(ent.Owner, ent.Comp, storage);
        }
        else
        {
            StopProcessing(ent.Owner, ent.Comp);
            return;
        }

        if (!ent.Comp.Processing)
            return;

        _hunger.ModifyHunger(occupant.Value, -ent.Comp.NutritionPerSecond);
        ent.Comp.NutrientAccumulator += ent.Comp.NutritionPerSecond;
        if (ent.Comp.NutrientAccumulator >= ent.Comp.NutrientPerMeat)
        {
            ent.Comp.NutrientAccumulator -= ent.Comp.NutrientPerMeat;
            Spawn(ent.Comp.MeatPrototype, Transform(ent).Coordinates);
        }
    }
}
