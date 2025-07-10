using System.Linq;
using Content.Shared.Chemistry.Components.SolutionManager;
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
        SubscribeLocalEvent<StorageComponent, EdibleEvent>(OnStorageEdible);
        SubscribeLocalEvent<ItemSlotsComponent, EdibleEvent>(OnItemSlotsEdible);
        SubscribeLocalEvent<OpenableComponent, EdibleEvent>(OnOpenableEdible);
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

    private void OnStorageEdible(Entity<StorageComponent> ent, ref EdibleEvent args)
    {
        // We don't care about the items stored inside if we're not destroying the container
        if (args.Cancelled || !args.Destroy)
            return;

        if (!ent.Comp.Container.ContainedEntities.Any())
            return;

        args.Cancelled = true;
        // TODO: Need a get nouns/verbs method
        _popup.PopupClient(Loc.GetString("food-has-used-storage", ("food", ent)), args.User, args.User);
    }

    private void OnItemSlotsEdible(Entity<ItemSlotsComponent> ent, ref EdibleEvent args)
    {
        // We don't care about the items stored inside if we're not destroying the container
        if (args.Cancelled || !args.Destroy)
            return;

        if (!ent.Comp.Slots.Any(slot => slot.Value.HasItem))
            return;

        args.Cancelled = true;
        // TODO: Need a get nouns/verbs method
        _popup.PopupClient(Loc.GetString("food-has-used-storage", ("food", ent)), args.User, args.User);
    }

    private void OnOpenableEdible(Entity<OpenableComponent> ent, ref EdibleEvent args)
    {
        if (_openable.IsClosed(ent, args.User, ent.Comp))
            args.Cancelled = true;
    }
}
