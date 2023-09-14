using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Verbs;
using Content.Shared.Examine;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Item;

public abstract class SharedItemSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private   readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private   readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemComponent, GetVerbsEvent<InteractionVerb>>(AddPickupVerb);
        SubscribeLocalEvent<ItemComponent, InteractHandEvent>(OnHandInteract, before: new []{typeof(SharedItemSystem)});
        SubscribeLocalEvent<ItemComponent, StackCountChangedEvent>(OnStackCountChanged);

        SubscribeLocalEvent<ItemComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ItemComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<ItemComponent, ExaminedEvent>(OnExamine);
    }

    #region Public API

    public void SetSize(EntityUid uid, int size, ItemComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.Size = size;
        Dirty(component);
    }

    public void SetHeldPrefix(EntityUid uid, string? heldPrefix, ItemComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (component.HeldPrefix == heldPrefix)
            return;

        component.HeldPrefix = heldPrefix;
        Dirty(component);
        VisualsChanged(uid);
    }

    /// <summary>
    ///     Copy all item specific visuals from another item.
    /// </summary>
    public void CopyVisuals(EntityUid uid, ItemComponent otherItem, ItemComponent? item = null)
    {
        if (!Resolve(uid, ref item))
            return;

        item.RsiPath = otherItem.RsiPath;
        item.InhandVisuals = otherItem.InhandVisuals;
        item.HeldPrefix = otherItem.HeldPrefix;

        Dirty(item);
        VisualsChanged(uid);
    }

    #endregion

    private void OnHandInteract(EntityUid uid, ItemComponent component, InteractHandEvent args)
    {
        if (args.Handled || _combatMode.IsInCombatMode(args.User))
            return;

        args.Handled = _handsSystem.TryPickup(args.User, uid, animateUser: false);
    }

    protected virtual void OnStackCountChanged(EntityUid uid, ItemComponent component, StackCountChangedEvent args)
    {
        if (!TryComp<StackComponent>(uid, out var stack))
            return;

        if (!_prototype.TryIndex<StackPrototype>(stack.StackTypeId, out var stackProto) ||
            stackProto.ItemSize is not { } size)
            return;

        SetSize(uid, args.NewCount * size, component);
    }

    private void OnHandleState(EntityUid uid, ItemComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ItemComponentState state)
            return;

        component.Size = state.Size;
        SetHeldPrefix(uid, state.HeldPrefix, component);
    }

    private void OnGetState(EntityUid uid, ItemComponent component, ref ComponentGetState args)
    {
        args.State = new ItemComponentState(component.Size, component.HeldPrefix);
    }

    private void AddPickupVerb(EntityUid uid, ItemComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Hands == null ||
            args.Using != null ||
            !args.CanAccess ||
            !args.CanInteract ||
            !_handsSystem.CanPickupAnyHand(args.User, args.Target, handsComp: args.Hands, item: component))
            return;

        InteractionVerb verb = new();
        verb.Act = () => _handsSystem.TryPickupAnyHand(args.User, args.Target, checkActionBlocker: false,
            handsComp: args.Hands, item: component);
        verb.Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/pickup.svg.192dpi.png"));

        // if the item already in a container (that is not the same as the user's), then change the text.
        // this occurs when the item is in their inventory or in an open backpack
        Container.TryGetContainingContainer(args.User, out var userContainer);
        if (Container.TryGetContainingContainer(args.Target, out var container) && container != userContainer)
            verb.Text = Loc.GetString("pick-up-verb-get-data-text-inventory");
        else
            verb.Text = Loc.GetString("pick-up-verb-get-data-text");

        args.Verbs.Add(verb);
    }

    private void OnExamine(EntityUid uid, ItemComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("item-component-on-examine-size",
            ("size", component.Size)));
    }

    /// <summary>
    ///     Notifies any entity that is holding or wearing this item that they may need to update their sprite.
    /// </summary>
    /// <remarks>
    ///     This is used for updating both inhand sprites and clothing sprites, but it's here just cause it needs to
    ///     be in one place.
    /// </remarks>
    public virtual void VisualsChanged(EntityUid owner)
    {
    }
}
