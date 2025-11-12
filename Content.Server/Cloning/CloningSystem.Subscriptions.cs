using Content.Server.Forensics;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Cloning.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Paper;
using Content.Shared.Stacks;
using Content.Shared.Speech.Components;
using Content.Shared.Storage;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Cloning;

/// <summary>
/// The part of item cloning responsible for copying over important components.
/// </summary>
/// <remarks>
/// These are all not part of their corresponding systems because we don't want systems every system to depend on a CloningSystem namespace import, which is still heavily coupled to med code.
/// TODO: Create a more generic "CopyEntity" method/event (probably in RT) that doesn't have this problem and then move all these subscriptions.
/// </remarks>
public sealed partial class CloningSystem
{
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly ForensicsSystem _forensics = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly VocalSystem _vocal = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        // These are used for <see cref="CopyItem"/>.
        // Anything not copied over here gets reverted to the values the item had in its prototype.
        // This method of copying items is of course not perfect as we cannot clone every single component, which would be pretty much impossible with our ECS.
        // We only consider the most important components so the paradox clone gets similar equipment.
        // This method of using subscriptions was chosen to make it easy for forks to add their own custom components that need to be copied.
        SubscribeLocalEvent<StackComponent, CloningItemEvent>(OnCloneItemStack);
        SubscribeLocalEvent<LabelComponent, CloningItemEvent>(OnCloneItemLabel);
        SubscribeLocalEvent<PaperComponent, CloningItemEvent>(OnCloneItemPaper);
        SubscribeLocalEvent<ForensicsComponent, CloningItemEvent>(OnCloneItemForensics);
        SubscribeLocalEvent<StoreComponent, CloningItemEvent>(OnCloneItemStore);

        // These are for cloning components that cannot be cloned using CopyComp.
        // Put them into CloningSettingsPrototype.EventComponents to have them be applied to the clone.
        SubscribeLocalEvent<VocalComponent, CloningEvent>(OnCloneVocal);
        SubscribeLocalEvent<StorageComponent, CloningEvent>(OnCloneStorage);
        SubscribeLocalEvent<InventoryComponent, CloningEvent>(OnCloneInventory);
        SubscribeLocalEvent<MovementSpeedModifierComponent, CloningEvent>(OnCloneInventory);
    }

    private void OnCloneItemStack(Entity<StackComponent> ent, ref CloningItemEvent args)
    {
        // if the clone is a stack as well, adjust the count of the copy
        if (TryComp<StackComponent>(args.CloneUid, out var cloneStackComp))
            _stack.SetCount((args.CloneUid, cloneStackComp), ent.Comp.Count);
    }

    private void OnCloneItemLabel(Entity<LabelComponent> ent, ref CloningItemEvent args)
    {
        // copy the label
        _label.Label(args.CloneUid, ent.Comp.CurrentLabel);
    }

    private void OnCloneItemPaper(Entity<PaperComponent> ent, ref CloningItemEvent args)
    {
        // copy the text and any stamps
        if (TryComp<PaperComponent>(args.CloneUid, out var clonePaperComp))
        {
            _paper.SetContent((args.CloneUid, clonePaperComp), ent.Comp.Content);
            _paper.CopyStamps(ent.AsNullable(), (args.CloneUid, clonePaperComp));
        }
    }

    private void OnCloneItemForensics(Entity<ForensicsComponent> ent, ref CloningItemEvent args)
    {
        // copy any forensics to the cloned item
        _forensics.CopyForensicsFrom(ent.Comp, args.CloneUid);
    }

    private void OnCloneItemStore(Entity<StoreComponent> ent, ref CloningItemEvent args)
    {
        // copy the current amount of currency in the store
        // at the moment this takes care of uplink implants and the portable nukie uplinks
        // turning a copied pda into an uplink will need some refactoring first
        if (TryComp<StoreComponent>(args.CloneUid, out var cloneStoreComp))
        {
            cloneStoreComp.Balance = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(ent.Comp.Balance);
        }
    }

    private void OnCloneVocal(Entity<VocalComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        _vocal.CopyComponent(ent.AsNullable(), args.CloneUid);
    }

    private void OnCloneStorage(Entity<StorageComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        _storage.CopyComponent(ent.AsNullable(), args.CloneUid);
    }

    private void OnCloneInventory(Entity<InventoryComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        _inventory.CopyComponent(ent.AsNullable(), args.CloneUid);
    }

    private void OnCloneInventory(Entity<MovementSpeedModifierComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        _movementSpeedModifier.CopyComponent(ent.AsNullable(), args.CloneUid);
    }
}
