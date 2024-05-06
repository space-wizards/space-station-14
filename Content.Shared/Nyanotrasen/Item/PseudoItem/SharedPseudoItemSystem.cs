using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Item.PseudoItem;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.Nyanotrasen.Item.PseudoItem;

public abstract partial class SharedPseudoItemSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    [ValidatePrototypeId<TagPrototype>]
    private const string PreventTag = "PreventLabel";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PseudoItemComponent, GetVerbsEvent<InnateVerb>>(AddInsertVerb);
        SubscribeLocalEvent<PseudoItemComponent, EntGotRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<PseudoItemComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);
        SubscribeLocalEvent<PseudoItemComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<PseudoItemComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<PseudoItemComponent, InteractionAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<PseudoItemComponent, PseudoItemInsertDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PseudoItemComponent, AttackAttemptEvent>(OnAttackAttempt);
    }

    private void AddInsertVerb(EntityUid uid, PseudoItemComponent component, GetVerbsEvent<InnateVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (component.Active)
            return;

        if (!TryComp<StorageComponent>(args.Target, out var targetStorage))
            return;

        if (!CheckItemFits((uid, component), (args.Target, targetStorage)))
            return;

        if (Transform(args.Target).ParentUid == uid)
            return;

        InnateVerb verb = new()
        {
            Act = () =>
            {
                TryInsert(args.Target, uid, component, targetStorage);
            },
            Text = Loc.GetString("action-name-insert-self"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private bool TryInsert(EntityUid storageUid, EntityUid toInsert, PseudoItemComponent component,
        StorageComponent? storage = null)
    {
        if (!Resolve(storageUid, ref storage))
            return false;

        if (!CheckItemFits((toInsert, component), (storageUid, storage)))
            return false;

        var itemComp = new ItemComponent
            { Size = component.Size, Shape = component.Shape, StoredOffset = component.StoredOffset };
        AddComp(toInsert, itemComp);
        _item.VisualsChanged(toInsert);

        _tag.TryAddTag(toInsert, PreventTag);

        if (!_storage.Insert(storageUid, toInsert, out _, null, storage))
        {
            component.Active = false;
            RemComp<ItemComponent>(toInsert);
            return false;
        }

        component.Active = true;
        return true;
    }

    private void OnEntRemoved(EntityUid uid, PseudoItemComponent component, EntGotRemovedFromContainerMessage args)
    {
        if (!component.Active)
            return;

        RemComp<ItemComponent>(uid);
        component.Active = false;
    }

    private void OnGettingPickedUpAttempt(EntityUid uid, PseudoItemComponent component,
        GettingPickedUpAttemptEvent args)
    {
        if (args.User == args.Item)
            return;

        Transform(uid).AttachToGridOrMap();
        args.Cancel();
    }

    private void OnDropAttempt(EntityUid uid, PseudoItemComponent component, DropAttemptEvent args)
    {
        if (component.Active)
            args.Cancel();
    }

    private void OnInsertAttempt(EntityUid uid, PseudoItemComponent component,
        ContainerGettingInsertedAttemptEvent args)
    {
        if (!component.Active)
            return;
        // This hopefully shouldn't trigger, but this is a failsafe just in case so we dont bluespace them cats
        args.Cancel();
    }

    // Prevents moving within the bag :)
    private void OnInteractAttempt(EntityUid uid, PseudoItemComponent component, InteractionAttemptEvent args)
    {
        if (args.Uid == args.Target && component.Active)
            args.Cancel();
    }

    private void OnDoAfter(EntityUid uid, PseudoItemComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used == null)
            return;

        args.Handled = TryInsert(args.Args.Used.Value, uid, component);
    }

    protected void StartInsertDoAfter(EntityUid inserter, EntityUid toInsert, EntityUid storageEntity,
        PseudoItemComponent? pseudoItem = null)
    {
        if (!Resolve(toInsert, ref pseudoItem))
            return;

        var ev = new PseudoItemInsertDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, inserter, 5f, ev, toInsert, toInsert, storageEntity)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnAttackAttempt(EntityUid uid, PseudoItemComponent component, AttackAttemptEvent args)
    {
        if (component.Active)
            args.Cancel();
    }
}
