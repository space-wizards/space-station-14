using Content.Server.Botany.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Botany.Systems;

/// <summary>
/// Harvest options for plants.
/// </summary>
public enum HarvestType
{
    /// <summary>
    /// Plant is removed on harvest.
    /// </summary>
    NoRepeat,

    /// <summary>
    /// Plant makes produce every Production ticks.
    /// </summary>
    Repeat,

    /// <summary>
    /// Repeat, plus produce is dropped on the ground near the plant automatically.
    /// </summary>
    SelfHarvest
}

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

        SubscribeLocalEvent<HarvestComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<HarvestComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnPlantGrow(Entity<HarvestComponent> ent, ref OnPlantGrowEvent args)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        PlantHolderComponent? plantHolder = null;
        PlantTraitsComponent? traits = null;
        if (!Resolve(uid, ref plantHolder, ref traits))
            return;

        if (plantHolder.Dead || plantHolder.Seed == null)
            return;

        // Check if plant is ready for harvest
        if (component.HarvestRepeat == HarvestType.Repeat || component.HarvestRepeat == HarvestType.SelfHarvest)
        {
            // Repeat harvest
            var timeSinceLastHarvest = plantHolder.Age - component.LastHarvestTime;
            if (timeSinceLastHarvest > traits.Production && !component.ReadyForHarvest)
            {
                component.ReadyForHarvest = true;
                plantHolder.UpdateSpriteAfterUpdate = true;
            }
        }
        else
        {
            // Non-repeat harvest
            if (plantHolder.Age > traits.Production && !component.ReadyForHarvest)
            {
                component.ReadyForHarvest = true;
                plantHolder.UpdateSpriteAfterUpdate = true;
            }
        }
    }

    private void OnInteractHand(Entity<HarvestComponent> ent, ref InteractHandEvent args)
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

    public void DoHarvest(Entity<HarvestComponent> ent)
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
            AfterHarvest((uid, plantHolder));
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
                component.LastHarvestTime = plantHolder.Age;
                plantHolder.Harvest = false;
                break;
        }

        AfterHarvest((uid, plantHolder));
    }

    private void AfterHarvest(Entity<PlantHolderComponent> ent)
    {
        var uid = ent.Owner;
        var component = ent.Comp;

        // Play scream sound if applicable
        if (TryComp<PlantTraitsComponent>(uid, out var traits) && traits.CanScream && component.Seed != null)
        {
            _audio.PlayPvs(component.Seed.ScreamSound, uid);
        }

        // Update sprite
        _plantHolder.UpdateSprite(uid, component);
    }

    /// <summary>
    /// Auto-harvests a plant.
    /// </summary>
    public void AutoHarvest(Entity<HarvestComponent> ent)
    {
        if (!ent.Comp.ReadyForHarvest)
            return;

        DoHarvest(ent);
    }
}
