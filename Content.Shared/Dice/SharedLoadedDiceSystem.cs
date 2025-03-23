using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Dice;

/// <summary>
///     Handles initializing loaded dice, and the UI to select a roll.
/// </summary>
public abstract class SharedLoadedDiceSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    // TODO REMOVE
    [Dependency] private readonly ILogManager _logManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoadedDiceComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<LoadedDiceComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<LoadedDiceComponent, LoadedDiceSideSelectedMessage>(OnSelected);
    }

    // TODO REMOVE
    private ISawmill _sawmill = default!;

    /// <summary>
    ///     If the entity is held in the hand or pocket, and the holder also has a loaded dice bag in the hand or in a pocket, return the holder
    /// </summary>
    private bool GetCapableUser(Entity<LoadedDiceComponent> entity, out EntityUid user)
    {
        user = default;

        if (!_container.TryGetContainingContainer((entity, null, null), out var container))
            return false;

        bool dieIsHeld = false;
        bool activatorIsHeld = entity.Comp.ActivatorWhitelist == null;

        foreach (var heldEntity in _inventory.GetHandOrInventoryEntities(container.Owner, SlotFlags.POCKET))
        {
            if (dieIsHeld && activatorIsHeld)
                break;
            else if (heldEntity == entity.Owner)
                dieIsHeld = true;
            else if (_whitelist.IsWhitelistPass(entity.Comp.ActivatorWhitelist, heldEntity))
                activatorIsHeld = true;
        }

        if (!dieIsHeld || !activatorIsHeld)
            return false;

        user = container.Owner;
        return true;
    }

    private void OnVerb(Entity<LoadedDiceComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!GetCapableUser(entity, out var user))
            return;

        if (user != args.User)
            return;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("loaded-dice-set-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => TryOpenUi(entity.Owner, user, entity.Comp)
        });
    }

    private void OnAlternativeVerb(EntityUid uid, LoadedDiceComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_container.TryGetContainingContainer((uid, null, null), out var container))
            return;

        // TODO: Check it's in hand of args.User

        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Priority = 1, // Make this the alt-click verb
            Text = Loc.GetString("loaded-dice-unset-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Act = () => SetSelectedSide(uid, component, null)
        });
    }

    private void OnSelected(EntityUid uid, LoadedDiceComponent component, LoadedDiceSideSelectedMessage args)
    {
        SetSelectedSide(uid, component, args.SelectedSide);
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, LoadedDiceComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!TryComp(user, out ActorComponent? actor))
            return;
        _uiSystem.TryToggleUi(uid, LoadedDiceUiKey.Key, actor.PlayerSession);
    }

    private void UpdateUi(EntityUid uid, DiceComponent? die = null, LoadedDiceComponent? component = null)
    {
        if (!Resolve(uid, ref die))
            return;

        if (!Resolve(uid, ref component))
            return;

        var state = new LoadedDiceBoundUserInterfaceState(die, component.SelectedSide);

        _uiSystem.SetUiState(uid, LoadedDiceUiKey.Key, state);
    }

    public void SetSelectedSide(EntityUid uid, LoadedDiceComponent? component = null, int? selectedSide = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!TryComp<DiceComponent>(uid, out var die))
            return;

        // Make sure that it is valid change
        if (selectedSide != null && (selectedSide < 1 || selectedSide > die.Sides))
            return;

        // TODO: Reimplement
        // There may not be a user if the change is not done by a player
        //if (thereisauser)
        //{
        //    string message;
        //    LogStringHandler adminMessage;
        //    if (selectedSide == null)
        //    {
        //        message = Loc.GetString("loaded-dice-unset", ("die", uid));
        //        adminMessage = $"{ToPrettyString(component.User):player} unset the loaded {ToPrettyString(uid):target}";
        //    }
        //    else
        //    {
        //        var sideValue = (selectedSide - die.Offset) * die.Multiplier;

        //        message = Loc.GetString("loaded-dice-set", ("die", uid), ("value", sideValue));
        //        adminMessage = $"{ToPrettyString(component.User):player} set the loaded {ToPrettyString(uid):target} to roll {sideValue}";
        //    }

        //    _popup.PopupEntity(message, uid, component.User.Value, PopupType.Small);
        //    _adminLogger.Add(LogType.Action, LogImpact.Low, ref adminMessage);
        //}

        component.SelectedSide = selectedSide;

        UpdateUi(uid, die, component);
        Dirty(uid, component);
    }
}
