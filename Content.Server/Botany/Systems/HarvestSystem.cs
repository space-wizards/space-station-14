using Content.Server.Botany.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

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

public sealed class HarvestSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HarvestComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<HarvestComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnPlantGrow(EntityUid uid, HarvestComponent component, OnPlantGrowEvent args)
    {
        if (!TryComp<PlantHolderComponent>(uid, out var plantHolder) ||
            !TryComp<PlantTraitsComponent>(uid, out var traits))
            return;

        if (plantHolder.Dead || plantHolder.Seed == null)
            return;

        // Check if plant is ready for harvest
        if (plantHolder.Age >= traits.Production)
        {
            var timeSinceLastHarvest = plantHolder.Age - component.LastHarvestTime;
            if (timeSinceLastHarvest >= traits.Production && !component.ReadyForHarvest)
            {
                component.ReadyForHarvest = true;
                plantHolder.UpdateSpriteAfterUpdate = true;
            }
        }
    }

    private void OnInteractHand(EntityUid uid, HarvestComponent component, InteractHandEvent args)
    {
        if (!TryComp<PlantHolderComponent>(uid, out var plantHolder) ||
            !TryComp<PlantTraitsComponent>(uid, out var traits))
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
        DoHarvest(uid, args.User, component, plantHolder, traits);
    }

    public void DoHarvest(EntityUid plantUid, EntityUid user, HarvestComponent? harvestComp = null,
        PlantHolderComponent? plantHolder = null, PlantTraitsComponent? traits = null)
    {
        if (!Resolve(plantUid, ref harvestComp, ref plantHolder, ref traits))
            return;

        if (plantHolder.Dead)
        {
            // Remove dead plant
            _plantHolder.RemovePlant(plantUid, plantHolder);
            AfterHarvest(plantUid, harvestComp, plantHolder);
            return;
        }

        if (!harvestComp.ReadyForHarvest)
            return;

        // Spawn products
        var yield = traits.Yield;
        if (plantHolder.Seed?.ProductPrototypes != null)
        {
            for (int i = 0; i < yield; i++)
            {
                foreach (var productPrototype in plantHolder.Seed.ProductPrototypes)
                {
                    var product = Spawn(productPrototype, Transform(plantUid).Coordinates);

                    // Apply mutations to product
                    if (TryComp<ProduceComponent>(product, out var produce))
                    {
                        _botany.ProduceGrown(product, produce);
                    }
                }
            }
        }

        // Handle harvest type
        switch (harvestComp.HarvestRepeat)
        {
            case HarvestType.NoRepeat:
                _plantHolder.RemovePlant(plantUid, plantHolder);
                break;
            case HarvestType.Repeat:
            case HarvestType.SelfHarvest:
                harvestComp.ReadyForHarvest = false;
                harvestComp.LastHarvestTime = plantHolder.Age;
                break;
        }

        AfterHarvest(plantUid, harvestComp, plantHolder);
    }

    private void AfterHarvest(EntityUid uid, HarvestComponent component, PlantHolderComponent plantHolder)
    {
        // Play scream sound if applicable
        if (TryComp<PlantTraitsComponent>(uid, out var traits) && traits.CanScream && plantHolder.Seed != null)
        {
            _audio.PlayPvs(plantHolder.Seed.ScreamSound, uid);
        }

        // Update sprite
        _plantHolder.UpdateSprite(uid, plantHolder);
    }

    public void AutoHarvest(EntityUid uid, HarvestComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.ReadyForHarvest)
            return;

        DoHarvest(uid, uid, component);
    }
}
