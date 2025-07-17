﻿using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;

namespace Content.Shared.Nutrition.EntitySystems;

public sealed partial class IngestionSystem
{
    [Dependency] private readonly OpenableSystem _openable = default!;

    public void InitializeBlockers()
    {
        SubscribeLocalEvent<UnremoveableComponent, IngestibleEvent>(OnUnremovableIngestion);
        SubscribeLocalEvent<IngestionBlockerComponent, ItemMaskToggledEvent>(OnBlockerMaskToggled);
        SubscribeLocalEvent<IngestionBlockerComponent, IngestionAttemptEvent>(OnIngestionBlockerAttempt);
        SubscribeLocalEvent<IngestionBlockerComponent, InventoryRelayedEvent<IngestionAttemptEvent>>(OnIngestionBlockerAttempt);

        // Edible Event
        SubscribeLocalEvent<EdibleComponent, EdibleEvent>(OnEdible);
        SubscribeLocalEvent<StorageComponent, EdibleEvent>(OnStorageEdible);
        SubscribeLocalEvent<ItemSlotsComponent, EdibleEvent>(OnItemSlotsEdible);
        SubscribeLocalEvent<OpenableComponent, EdibleEvent>(OnOpenableEdible);

        // Digestion Events
        SubscribeLocalEvent<EdibleComponent, IsDigestibleEvent>(OnEdibleIsDigestible);
        //SubscribeLocalEvent<DrainableSolutionComponent, IsDigestibleEvent>(OnDrainableIsDigestible);
        //SubscribeLocalEvent<PuddleComponent, IsDigestibleEvent>(OnPuddleIsDigestible);
    }

    private void OnUnremovableIngestion(Entity<UnremoveableComponent> entity, ref IngestibleEvent args)
    {
        // If we can't remove it we probably shouldn't be able to eat it.
        // TODO: Separate glue and Unremovable component.
        args.Cancelled = true;
    }

    private void OnBlockerMaskToggled(Entity<IngestionBlockerComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.Enabled = !args.Mask.Comp.IsToggled;
    }

    private void OnIngestionBlockerAttempt(Entity<IngestionBlockerComponent> entity, ref IngestionAttemptEvent args)
    {
        if (!args.Cancelled && entity.Comp.Enabled)
            args.Cancelled = true;
    }

    /// <summary>
    ///     Block ingestion attempts based on the equipped mask or head-wear
    /// </summary>
    private void OnIngestionBlockerAttempt(Entity<IngestionBlockerComponent> entity, ref InventoryRelayedEvent<IngestionAttemptEvent> args)
    {
        if (args.Args.Cancelled || !entity.Comp.Enabled)
            return;

        args.Args.Cancelled = true;
        args.Args.Blocker = entity;
    }

    private void OnEdible(Entity<EdibleComponent> entity, ref EdibleEvent args)
    {
        if (args.Cancelled || args.Solution != null)
            return;

        if (entity.Comp.UtensilRequired && !HasRequiredUtensils(args.User, entity.Comp.Utensil))
        {
            args.Cancelled = true;
            return;
        }

        // Check this last
        if (!_solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out args.Solution) || IsEmpty(entity))
        {
            args.Cancelled = true;

            _popup.PopupClient(Loc.GetString("ingestion-try-use-is-empty", ("entity", entity)), entity, args.User);
            return;
        }

        // Time is additive because I said so.
        args.Time += entity.Comp.Delay;
    }

    private void OnStorageEdible(Entity<StorageComponent> ent, ref EdibleEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Container.ContainedEntities.Any())
            return;

        args.Cancelled = true;

        _popup.PopupClient(Loc.GetString("edible-has-used-storage", ("food", ent), ("verb", GetEdibleVerb(ent))), args.User, args.User);
    }

    private void OnItemSlotsEdible(Entity<ItemSlotsComponent> ent, ref EdibleEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Slots.Any(slot => slot.Value.HasItem))
            return;

        args.Cancelled = true;

        _popup.PopupClient(Loc.GetString("edible-has-used-storage", ("food", ent), ("verb", GetEdibleVerb(ent))), args.User, args.User);
    }

    private void OnOpenableEdible(Entity<OpenableComponent> ent, ref EdibleEvent args)
    {
        if (_openable.IsClosed(ent, args.User, ent.Comp))
            args.Cancelled = true;
    }

    private void OnEdibleIsDigestible(Entity<EdibleComponent> ent, ref IsDigestibleEvent args)
    {
        // This is a super hacky solution in place of having a proper component that declares something as being
        // a solution of purely drinkable reagents with nothing to stop us. Is it drainable? Probably not!
        if (ent.Comp.EdibleType == EdibleType.Drink)
        {
            args.UniversalDigestion();
            return;
        }

        if (ent.Comp.RequireDead && _mobState.IsAlive(ent))
            return;

        args.AddDigestible(ent.Comp.RequiresSpecialDigestion);
    }
}
