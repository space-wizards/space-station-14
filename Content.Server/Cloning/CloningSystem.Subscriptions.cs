using Content.Server.Forensics;
using Content.Shared.Cloning.Events;
using Content.Shared.Clothing.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Stacks;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Cloning;

/// <summary>
///     The part of item cloning responsible for copying over important components.
///     This is used for <see cref="CopyItem"/>.
///     Anything not copied over here gets reverted to the values the item had in its prototype.
/// </summary>
/// <remarks>
///     This method of copying items is of course not perfect as we cannot clone every single component, which would be pretty much impossible with our ECS.
///     We only consider the most important components so the paradox clone gets similar equipment.
///     This method of using subscriptions was chosen to make it easy for forks to add their own custom components that need to be copied.
/// </remarks>
public sealed partial class CloningSystem : EntitySystem
{
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedLabelSystem _label = default!;
    [Dependency] private readonly ForensicsSystem _forensics = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StackComponent, CloningItemEvent>(OnCloneStack);
        SubscribeLocalEvent<LabelComponent, CloningItemEvent>(OnCloneLabel);
        SubscribeLocalEvent<PaperComponent, CloningItemEvent>(OnClonePaper);
        SubscribeLocalEvent<ForensicsComponent, CloningItemEvent>(OnCloneForensics);
        SubscribeLocalEvent<StoreComponent, CloningItemEvent>(OnCloneStore);
    }

    private void OnCloneStack(Entity<StackComponent> ent, ref CloningItemEvent args)
    {
        // if the clone is a stack as well, adjust the count of the copy
        if (TryComp<StackComponent>(args.CloneUid, out var cloneStackComp))
            _stack.SetCount(args.CloneUid, ent.Comp.Count, cloneStackComp);
    }

    private void OnCloneLabel(Entity<LabelComponent> ent, ref CloningItemEvent args)
    {
        // copy the label
        _label.Label(args.CloneUid, ent.Comp.CurrentLabel);
    }

    private void OnClonePaper(Entity<PaperComponent> ent, ref CloningItemEvent args)
    {
        // copy the text and any stamps
        if (TryComp<PaperComponent>(args.CloneUid, out var clonePaperComp))
        {
            _paper.SetContent((args.CloneUid, clonePaperComp), ent.Comp.Content);
            _paper.CopyStamps(ent.AsNullable(), (args.CloneUid, clonePaperComp));
        }
    }

    private void OnCloneForensics(Entity<ForensicsComponent> ent, ref CloningItemEvent args)
    {
        // copy any forensics to the cloned item
        _forensics.CopyForensicsFrom(ent.Comp, args.CloneUid);
    }

    private void OnCloneStore(Entity<StoreComponent> ent, ref CloningItemEvent args)
    {
        // copy the current amount of currency in the store
        // at the moment this takes care of uplink implants and the portable nukie uplinks
        // turning a copied pda into an uplink will need some refactoring first
        if (TryComp<StoreComponent>(args.CloneUid, out var cloneStoreComp))
        {
            cloneStoreComp.Balance = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(ent.Comp.Balance);
        }
    }

}
