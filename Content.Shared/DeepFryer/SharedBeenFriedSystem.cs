using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeepFryer.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Nuke;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Slippery;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.DeepFryer;

public abstract class SharedBeenFriedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeenFriedComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<BeenFriedComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        SubscribeLocalEvent<BeenFriedComponent, FlavorProfileModificationEvent>(OnFlavorProfileModifiers);
        // Cancel pretty much every use for this except for ingestion and storage
        SubscribeLocalEvent<BeenFriedComponent, GettingUsedAttemptEvent>(CancelUse);
        SubscribeLocalEvent<BeenFriedComponent, ToolUseAttemptEvent>(CancelToolUse);
        SubscribeLocalEvent<BeenFriedComponent, AttemptMeleeEvent>(CancelMelee);
        SubscribeLocalEvent<BeenFriedComponent, AttemptShootEvent>(CancelShoot);
        SubscribeLocalEvent<BeenFriedComponent, BeingEquippedAttemptEvent>(CancelEquip);
        SubscribeLocalEvent<BeenFriedComponent, SlipCausingAttemptEvent>(CancelSlip);
        SubscribeLocalEvent<BeenFriedComponent, ItemSlotInsertAttemptEvent>(CancelSlot);
    }

    private void OnExamine(Entity<BeenFriedComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(BeenFriedComponent)))
        {
            args.PushMarkup(Loc.GetString("fried-on-examine-details"));
        }
    }

    private void OnRefreshNameModifiers(Entity<BeenFriedComponent> ent, ref RefreshNameModifiersEvent args)
    {
        args.AddModifier("fried-name-prefix");
    }

    private void OnFlavorProfileModifiers(Entity<BeenFriedComponent> ent, ref FlavorProfileModificationEvent args)
    {
        args.Flavors.Add("fried");
    }

    // Note: Currently, this also cancels storage into backpacks via placement on the icon. Opening the backpack and placing it still works. Not exactly sure how to fix that...
    private void CancelUse(Entity<BeenFriedComponent> ent, ref GettingUsedAttemptEvent args)
    {
        // If it isn't for eating, storing, or nuking something, it no longer works
        if (!HasComp<EdibleComponent>(ent) && !HasComp<StorageComponent>(ent) && !HasComp<NukeDiskComponent>(ent))
            args.Cancel();
    }

    private void CancelToolUse(Entity<BeenFriedComponent> ent, ref ToolUseAttemptEvent args)
    {
        // Allows plushies to be opened back up — no frying the nuke disk into one
        if (!HasComp<SecretStashComponent>(ent))
            args.Cancel();
    }

    private void CancelMelee(Entity<BeenFriedComponent> ent, ref AttemptMeleeEvent args)
    {
        args.Cancelled = true;
    }

    private void CancelShoot(Entity<BeenFriedComponent> ent, ref AttemptShootEvent args)
    {
        args.Cancelled = true;
    }

    private void CancelEquip(Entity<BeenFriedComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void CancelSlip(Entity<BeenFriedComponent> ent, ref SlipCausingAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void CancelSlot(Entity<BeenFriedComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        // Allow the nuke disk to be inserted, but nothing else
        if (!HasComp<NukeDiskComponent>(ent))
            args.Cancelled = true;
    }
}
