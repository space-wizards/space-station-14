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
    [Dependency] private readonly PlantHolderSystem _holder = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlantHarvestComponent, OnPlantGrowEvent>(OnPlantGrow);
        SubscribeLocalEvent<PlantHarvestComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlantHarvestComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnPlantGrow(Entity<PlantHarvestComponent> ent, ref OnPlantGrowEvent args)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantHolderComponent? holder)
            || !TryComp(uid, out PlantComponent? plant))
            return;

        if (component is { ReadyForHarvest: true, HarvestRepeat: HarvestType.SelfHarvest })
            AutoHarvest((ent, ent, holder));

        // Check if plant is ready for harvest.
        var timeLastHarvest = holder.Age - component.LastHarvest;
        if (timeLastHarvest > plant.Production && !component.ReadyForHarvest)
        {
            component.ReadyForHarvest = true;
            component.LastHarvest = holder.Age;
            holder.UpdateSpriteAfterUpdate = true;
        }
    }

    private void OnInteractUsing(Entity<PlantHarvestComponent> ent, ref InteractUsingEvent args)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantTraitsComponent? traits)
            || !traits.Ligneous
            || !TryComp(uid, out PlantHolderComponent? holder)
            || holder.Seed == null)
            return;

        if (!component.ReadyForHarvest || holder.Dead || holder.Seed == null)
            return;

        var canHarvestUsing = _botany.CanHarvest(holder.Seed, args.Used);
        HandleInteraction((ent, ent, holder), args.User, !canHarvestUsing);
    }

    private void OnInteractHand(Entity<PlantHarvestComponent> ent, ref InteractHandEvent args)
    {
        if (!TryComp(ent, out PlantHolderComponent? holder)
            || !TryComp(ent, out PlantTraitsComponent? traits))
            return;

        HandleInteraction((ent, ent, holder), args.User, traits.Ligneous);
    }

    private void HandleInteraction(
        Entity<PlantHarvestComponent, PlantHolderComponent> ent,
        EntityUid user,
        bool missingRequiredTool
    )
    {
        if (missingRequiredTool)
        {
            _popup.PopupCursor(Loc.GetString("plant-holder-component-ligneous-cant-harvest-message"), user);
            return;
        }

        var (_, harvest, holder) = ent;
        if (!harvest.ReadyForHarvest || holder.Dead || holder.Seed == null)
            return;

        // Perform harvest.
        DoHarvest(ent, user);
    }

    public void DoHarvest(Entity<PlantHarvestComponent> ent, EntityUid user)
    {
        var (uid, component) = ent;

        if (!TryComp(uid, out PlantHolderComponent? holder)
            || !TryComp(uid, out PlantTraitsComponent? traits))
            return;

        if (holder.Dead)
        {
            // Remove dead plant.
            _holder.RemovePlant(uid, holder);
            AfterHarvest(ent);
            return;
        }

        if (!component.ReadyForHarvest)
            return;

        // Spawn products.
        if (holder.Seed != null)
            _botany.Harvest(holder.Seed, user, ent);

        // Handle harvest type.
        if (component.HarvestRepeat == HarvestType.NoRepeat)
            _holder.RemovePlant(uid, holder);

        AfterHarvest(ent, holder, traits);
    }

    private void AfterHarvest(Entity<PlantHarvestComponent> ent, PlantHolderComponent? holder = null, PlantTraitsComponent? traits = null)
    {
        var (uid, component) = ent;
        if (!Resolve(uid, ref traits, ref holder))
            return;

        component.ReadyForHarvest = false;
        component.LastHarvest = holder.Age;

        // Play scream sound if applicable.
        if (traits.CanScream && holder.Seed != null)
            _audio.PlayPvs(holder.Seed.ScreamSound, uid);

        // Update sprite.
        _holder.UpdateSprite(uid, holder);
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
