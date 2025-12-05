using Content.Server.Botany.Components;
using Content.Server.Hands.Systems;
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
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlantHarvestComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantHarvestComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnPlantGrow(Entity<PlantHarvestComponent> ent, ref OnPlantGrowEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (plantHolder.Dead || plantHolder.Seed == null)
            return;

        if (component.ReadyForHarvest && component.HarvestRepeat == HarvestType.SelfHarvest)
            AutoHarvest(ent);

        // Check if plant is ready for harvest
        var timeLastHarvest = plantHolder.Age - component.LastHarvest;
        if (timeLastHarvest > traits.Production && !component.ReadyForHarvest)
        {
            component.ReadyForHarvest = true;
            component.LastHarvest = plantHolder.Age;
            plantHolder.UpdateSpriteAfterUpdate = true;
        }
    }

    private void OnInteractHand(Entity<PlantHarvestComponent> ent, ref InteractHandEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (!component.ReadyForHarvest || plantHolder.Dead)
            return;

        // Check if sharp tool is required
        if (traits.Ligneous)
        {
            if (!_hands.TryGetActiveItem(args.User, out var activeItem) ||
                plantHolder.Seed == null ||
                !_botany.CanHarvest(plantHolder.Seed, activeItem))
            {
                _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), args.User);
                return;
            }
        }

        // Perform harvest
        DoHarvest(ent);
    }

    public void DoHarvest(Entity<PlantHarvestComponent> ent)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (plantHolder.Dead)
        {
            // Remove dead plant
            _plantHolder.RemovePlant(uid, plantHolder);
            AfterHarvest(ent);
            return;
        }

        if (!component.ReadyForHarvest)
            return;

        // Spawn products
        var yield = traits.Yield;
        if (plantHolder.Seed?.ProductPrototypes != null)
        {
            for (var i = 0; i < yield; i++)
            {
                foreach (var productPrototype in plantHolder.Seed.ProductPrototypes)
                {
                    var product = Spawn(productPrototype, Transform(uid).Coordinates);

                    // Apply mutations to product
                    if (TryComp<ProduceComponent>(product, out var produce))
                    {
                        _botany.ProduceGrown(product, produce);
                    }
                }
            }
        }

        // Handle harvest type
        switch (component.HarvestRepeat)
        {
            case HarvestType.NoRepeat:
                _plantHolder.RemovePlant(uid, plantHolder);
                break;
            case HarvestType.Repeat:
            case HarvestType.SelfHarvest:
                component.ReadyForHarvest = false;
                component.LastHarvest = plantHolder.Age;
                break;
        }

        AfterHarvest(ent);
    }

    private void AfterHarvest(Entity<PlantHarvestComponent> ent)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        PlantTraitsComponent? traits = null;
        PlantHolderComponent? plantHolder = null;
        if (!Resolve(uid, ref traits, ref plantHolder))
            return;

        component.ReadyForHarvest = false;
        component.LastHarvest = plantHolder.Age;

        // Play scream sound if applicable
        if (traits.CanScream && plantHolder.Seed != null)
            _audio.PlayPvs(plantHolder.Seed.ScreamSound, uid);

        // Update sprite
        _plantHolder.UpdateSprite(uid, plantHolder);
    }

    /// <summary>
    /// Auto-harvests a plant.
    /// </summary>
    public void AutoHarvest(Entity<PlantHarvestComponent> ent)
    {
        if (!ent.Comp.ReadyForHarvest)
            return;

        AfterHarvest(ent);
    }
}
