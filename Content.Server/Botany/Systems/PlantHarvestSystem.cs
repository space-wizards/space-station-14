using JetBrains.Annotations;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Botany;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.Popups;
using Content.Shared.Random;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Manages harvest readiness and execution for plants, including repeat/self-harvest
/// logic and produce spawning, responding to growth and interaction events.
/// </summary>
public sealed class HarvestSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PlantTraySystem _tray = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RandomHelperSystem _randomHelper = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantHarvestComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantHarvestComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlantHarvestComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnPlantGrow(Entity<PlantHarvestComponent> ent, ref OnPlantGrowEvent args)
    {
        var (plantUid, component) = ent;
        var (trayUid, tray) = args.Tray;

        if (!TryComp<PlantHolderComponent>(plantUid, out var holder)
            || !TryComp<PlantComponent>(plantUid, out var plant)
            || holder.Dead)
            return;

        // Check if plant is ready for harvest.
        var timeLastHarvest = holder.Age - component.LastHarvest;
        if (timeLastHarvest > plant.Production && !component.ReadyForHarvest)
        {
            component.ReadyForHarvest = true;
            component.LastHarvest = holder.Age;
            tray.UpdateSpriteAfterUpdate = true;
            TryAutoHarvest(ent, args.Tray, trayUid);
        }
    }

    private void OnInteractUsing(Entity<PlantHarvestComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var (plantUid, harvest) = ent;
        var trayUid = Transform(plantUid).ParentUid;

        if (!TryComp<PlantTrayComponent>(trayUid, out var tray)
            || !TryComp<PlantHolderComponent>(plantUid, out var holder)
            || !TryComp<PlantTraitsComponent>(plantUid, out var traits))
            return;

        if (!harvest.ReadyForHarvest || holder.Dead || !traits.Ligneous)
            return;

        // ligneous requires sharp tool.
        if (!HasComp<SharpComponent>(args.Used))
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
            return;
        }

        TryHandleHarvest((plantUid, harvest), (trayUid, tray), args.User);
        args.Handled = true;
    }

    private void OnInteractHand(Entity<PlantHarvestComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var (plantUid, harvest) = ent;
        var trayUid = Transform(plantUid).ParentUid;

        if (!TryComp<PlantTrayComponent>(trayUid, out var tray)
            || !TryComp<PlantHolderComponent>(plantUid, out var holder)
            || !TryComp<PlantTraitsComponent>(plantUid, out var traits))
            return;

        if (!harvest.ReadyForHarvest || holder.Dead)
            return;

        if (traits.Ligneous)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
            return;
        }

        TryHandleHarvest((plantUid, harvest), (trayUid, tray), args.User);
        args.Handled = true;
    }

    private void TryHandleHarvest(Entity<PlantHarvestComponent> ent, Entity<PlantTrayComponent> trayEnt, EntityUid user)
    {
        if (TryComp<PlantDataComponent>(ent.Owner, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(plantData.DisplayName):seed} at Pos:{Transform(trayEnt.Owner).Coordinates}.");

        DoHarvest(ent, trayEnt, user);
    }

    private void TryAutoHarvest(Entity<PlantHarvestComponent> ent, Entity<PlantTrayComponent> trayEnt, EntityUid user)
    {
        if (ent.Comp.HarvestRepeat != HarvestType.SelfHarvest)
            return;

        if (TryComp<PlantDataComponent>(ent.Owner, out var plantData) && plantData.HarvestLogImpact != null)
            _adminLogger.Add(LogType.Botany, plantData.HarvestLogImpact.Value, $"Auto-harvested {Loc.GetString(plantData.DisplayName):seed} at Pos:{Transform(trayEnt.Owner).Coordinates}.");

        DoHarvest(ent, trayEnt, user);
    }

    /// <summary>
    /// Harvests the plant and produces the produce.
    /// </summary>
    /// <param name="ent">The plant harvest component.</param>
    /// <param name="trayEnt">The plant tray component.</param>
    /// <param name="user">The user who is harvesting the plant.</param>
    [PublicAPI]
    public void DoHarvest(Entity<PlantHarvestComponent> ent, Entity<PlantTrayComponent> trayEnt, EntityUid user)
    {
        var (plantUid, harvest) = ent;
        var (trayUid, _) = trayEnt;

        if (!TryComp<PlantComponent>(plantUid, out var plant)
            || !TryComp<PlantDataComponent>(plantUid, out var plantData)
            || !TryComp<PlantTraitsComponent>(plantUid, out var traits)
            || !TryComp<PlantHolderComponent>(plantUid, out var holder))
            return;

        if (holder.Dead)
        {
            _tray.RemovePlant(trayUid);
            return;
        }

        if (!harvest.ReadyForHarvest || plantData.ProductPrototypes.Count == 0 || plant.Yield == 0)
            return;

        var name = Loc.GetString(plantData.DisplayName);
        _popup.PopupCursor(Loc.GetString("botany-harvest-success-message", ("name", name)), user, PopupType.Medium);

        var totalYield = 0;
        if (plant.Yield >= 0)
        {
            totalYield = holder.YieldMod < 0 ? plant.Yield : plant.Yield * holder.YieldMod;
            totalYield = Math.Max(1, totalYield);
        }

        var position = Transform(trayUid).Coordinates;
        for (var i = 0; i < totalYield; i++)
        {
            var product = _random.Pick(plantData.ProductPrototypes);
            var entity = Spawn(product, position);
            _randomHelper.RandomOffset(entity, 0.25f);

            var produce = EnsureComp<ProduceComponent>(entity);
            produce.PlantProtoId = MetaData(plantUid).EntityPrototype!.ID;
            produce.PlantData = _botany.ClonePlantSnapshotData(plantUid);
            _botany.ProduceGrown(entity, produce);
            _appearance.SetData(entity, ProduceVisuals.Potency, plant.Potency);
        }

        harvest.ReadyForHarvest = false;
        harvest.LastHarvest = holder.Age;

        if (traits.CanScream)
            _audio.PlayPvs(new SoundCollectionSpecifier("PlantScreams"), trayUid);

        if (harvest.HarvestRepeat == HarvestType.NoRepeat)
            _tray.RemovePlant(trayUid);

        _tray.UpdateSprite(trayEnt.AsNullable());
    }
}
