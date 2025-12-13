using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Manages harvest readiness and execution for plants, including repeat/self-harvest
/// logic and produce spawning, responding to growth and interaction events.
/// </summary>
public sealed class HarvestSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantHarvestComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantHarvestComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlantHarvestComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnPlantGrow(Entity<PlantHarvestComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (plantHolder.Dead || plantHolder.Seed == null)
            return;

        if (component is { ReadyForHarvest: true, HarvestRepeat: HarvestType.SelfHarvest })
            AutoHarvest((ent, ent, plantHolder));

        // Check if plant is ready for harvest.
        var timeLastHarvest = plantHolder.Age - component.LastHarvest;
        if (timeLastHarvest > traits.Production && !component.ReadyForHarvest)
        {
            component.ReadyForHarvest = true;
            component.LastHarvest = plantHolder.Age;
            plantHolder.UpdateSpriteAfterUpdate = true;
        }
    }

    private void OnInteractUsing(Entity<PlantHarvestComponent> ent, ref InteractUsingEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (!component.ReadyForHarvest || plantHolder.Dead || plantHolder.Seed == null || !traits.Ligneous)
            return;

        // Check if sharp tool is required.
        if (!_botany.CanHarvest(plantHolder.Seed, args.Used))
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
            return;
        }

        // Perform harvest.
        DoHarvest(ent, args.User);
    }

    private void OnInteractHand(Entity<PlantHarvestComponent> ent, ref InteractHandEvent args)
    {
        var (uid, component) = ent;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (!component.ReadyForHarvest || plantHolder.Dead || plantHolder.Seed == null)
            return;

        // Check if sharp tool is required.
        if (traits.Ligneous)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
            return;
        }

        // Perform harvest.
        DoHarvest(ent, args.User);
    }

    public void DoHarvest(Entity<PlantHarvestComponent> ent, EntityUid user)
    {
        var (uid, component) = ent;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (plantHolder.Dead)
        {
            // Remove dead plant.
            _plantHolder.RemovePlant(uid, plantHolder);
            AfterHarvest(ent);
            return;
        }

        if (!component.ReadyForHarvest)
            return;

        // Spawn products.
        if (plantHolder.Seed != null)
            _botany.Harvest(plantHolder.Seed, user, ent);

        // Handle harvest type.
        if (component.HarvestRepeat == HarvestType.NoRepeat)
            _plantHolder.RemovePlant(uid, plantHolder);

        AfterHarvest(ent, plantHolder, traits);
    }

    private void AfterHarvest(Entity<PlantHarvestComponent> ent, PlantHolderComponent? plantHolder = null, PlantTraitsComponent? traits = null)
    {
        var (uid, component) = ent;
        if (!Resolve(uid, ref traits, ref plantHolder))
            return;

        component.ReadyForHarvest = false;
        component.LastHarvest = plantHolder.Age;

        // Play scream sound if applicable.
        if (traits.CanScream && plantHolder.Seed != null)
            _audio.PlayPvs(plantHolder.Seed.ScreamSound, uid);

        // Update sprite.
        _plantHolder.UpdateSprite(uid, plantHolder);
    }

    /// <summary>
    /// Auto-harvests a plant.
    /// </summary>
    public void AutoHarvest(Entity<PlantHarvestComponent, PlantHolderComponent> ent)
    {
        if (!ent.Comp1.ReadyForHarvest || ent.Comp2.Seed == null)
            return;

        _botany.AutoHarvest(ent.Comp2.Seed, Transform(ent.Owner).Coordinates, ent);
        AfterHarvest(ent);
    }
}
