using Content.Shared.Cloning.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics.Systems;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Stacks;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Zombies;
using Content.Server.Zombies;

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
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedChameleonClothingSystem _chameleonClothing = default!;
    [Dependency] private SharedForensicsSystem _forensics = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private ZombieSystem _zombie = default!;

    // These are used for <see cref="CopyItem"/>.
    // Anything not copied over here gets reverted to the values the item had in its prototype.
    // This method of copying items is of course not perfect as we cannot clone every single component, which would be pretty much impossible with our ECS.
    // We only consider the most important components so the paradox clone gets similar equipment.
    // This method of using subscriptions was chosen to make it easy for forks to add their own custom components that need to be copied.
    [SubscribeLocalEvent]
    private void OnCloneItemStack(Entity<StackComponent> ent, ref ClonedEvent args)
    {
        // if the clone is a stack as well, adjust the count of the copy
        if (TryComp<StackComponent>(args.CloneUid, out var cloneStackComp))
            _stack.SetCount((args.CloneUid, cloneStackComp), ent.Comp.Count);
    }

    [SubscribeLocalEvent]
    private void OnCloneItemLabel(Entity<LabelComponent> ent, ref ClonedEvent args)
    {
        // copy the label
        _label.Label(args.CloneUid, ent.Comp.CurrentLabel);
    }

    [SubscribeLocalEvent]
    private void OnCloneItemPaper(Entity<PaperComponent> ent, ref ClonedEvent args)
    {
        // copy the text and any stamps
        if (TryComp<PaperComponent>(args.CloneUid, out var clonePaperComp))
        {
            _paper.SetContent((args.CloneUid, clonePaperComp), ent.Comp.Content);
            _paper.CopyStamps(ent.AsNullable(), (args.CloneUid, clonePaperComp));
        }
    }

    [SubscribeLocalEvent]
    private void OnForensicsCloned(Entity<ForensicsComponent> ent, ref ClonedEvent args)
    {
        // copy any forensics to the cloned item
        _forensics.CopyForensicsFrom(ent.AsNullable(), args.CloneUid);
    }

    [SubscribeLocalEvent]
    private void OnStoreCloned(Entity<StoreComponent> ent, ref ClonedEvent args)
    {
        // copy the current amount of currency in the store
        // at the moment this takes care of uplink implants and the portable nukie uplinks
        // turning a copied pda into an uplink will need some refactoring first
        if (TryComp<StoreComponent>(args.CloneUid, out var cloneStoreComp))
        {
            cloneStoreComp.Balance = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(ent.Comp.Balance);
        }
    }

    [SubscribeLocalEvent]
    private void OnChameleonClothingCloned(Entity<ChameleonClothingComponent> ent, ref ClonedEvent args)
    {
        // copy the prototype the original is mimicing
        _chameleonClothing.SetSelectedPrototype(args.CloneUid, ent.Comp.Default);
    }

    [SubscribeLocalEvent]
    private void OnZombieCloned(Entity<ZombieComponent> ent, ref ClonedEvent args)
    {
        // Return the original's appearance to how it was before being zombified.
        _zombie.UnZombify(ent, args.CloneUid, ent.Comp);
    }
}
