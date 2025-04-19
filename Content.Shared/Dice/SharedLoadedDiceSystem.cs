using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Dice;

/// <summary>
///     Handles loaded dice, including UI to select a roll, and overriding the roll when set.
/// </summary>
public abstract class SharedLoadedDiceSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LoadedDiceComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<LoadedDiceComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<LoadedDiceComponent, LoadedDiceSideSelectedMessage>(OnSelected);
        SubscribeLocalEvent<LoadedDiceComponent, DiceRollEvent>(OnDiceRoll);
    }

    private void OnDiceRoll(Entity<LoadedDiceComponent> entity, ref DiceRollEvent roll)
    {
        if (entity.Comp.SelectedSide == null)
            return;

        roll.Roll = entity.Comp.SelectedSide.Value;
    }

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

        if (!TryComp(entity, out DiceComponent? die))
            return;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("loaded-dice-set-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => TryOpenUi((entity.Owner, die, entity.Comp), user)
        });
    }

    private void OnAlternativeVerb(Entity<LoadedDiceComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!GetCapableUser(entity, out var user) || user != args.User)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Priority = 1, // Make this the alt-click verb
            Text = Loc.GetString("loaded-dice-unset-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/die.svg.192dpi.png")),
            Act = () => SetSelectedSide((entity, null, entity.Comp), null, user)
        });
    }

    private void OnSelected(Entity<LoadedDiceComponent> entity, ref LoadedDiceSideSelectedMessage args)
    {
        SetSelectedSide((entity.Owner, null, entity.Comp), args.SelectedSide, args.Actor);
    }

    private void TryOpenUi(Entity<DiceComponent, LoadedDiceComponent> entity, EntityUid user)
    {
        if (!TryComp(user, out ActorComponent? actor))
            return;

        _uiSystem.TryToggleUi(entity.Owner, LoadedDiceUiKey.Key, actor.PlayerSession);
        UpdateUi(entity);
    }

    private void UpdateUi(Entity<DiceComponent, LoadedDiceComponent> entity)
    {
        var state = new LoadedDiceBoundUserInterfaceState(entity.Comp1, entity.Comp2.SelectedSide);

        _uiSystem.SetUiState(entity.Owner, LoadedDiceUiKey.Key, state);
    }

    /// <summary>
    ///     Sets the selected side on the loaded die, ensuring the change is valid.
    ///     If selectedSide is null, this unsets the die, and makes it behave like a normal die.
    ///     If user is not null, this will also display a popup and log an admin message.
    /// </summary>
    public void SetSelectedSide(Entity<DiceComponent?, LoadedDiceComponent?> entity, int? selectedSide, EntityUid? user = null)
    {
        entity.Deconstruct(out var uid, out var die, out var loadedDie);

        if (!Resolve(uid, ref die, false))
            return;

        if (!Resolve(uid, ref loadedDie, false))
            return;

        // Make sure that it's a valid change
        if (selectedSide != null && (selectedSide < 1 || selectedSide > die.Sides))
            return;

        // There may not be a user if the change is not done by a player
        if (user != null)
        {
            if (!GetCapableUser((uid, loadedDie), out var capableUser) || capableUser != user)
            {
                // The user currently trying to set the die can't actually do that, close the UI
                _uiSystem.CloseUi((uid, null), LoadedDiceUiKey.Key, user);
                return;
            }

            string message;
            LogStringHandler adminMessage;
            if (selectedSide == null)
            {
                message = Loc.GetString("loaded-dice-unset", ("die", uid));
                adminMessage = $"{ToPrettyString(user):player} unset the loaded {ToPrettyString(uid):target}";
            }
            else
            {
                var sideValue = (selectedSide - die.Offset) * die.Multiplier;

                message = Loc.GetString("loaded-dice-set", ("die", uid), ("value", sideValue));
                adminMessage = $"{ToPrettyString(user):player} set the loaded {ToPrettyString(uid):target} to roll {sideValue}";
            }

            _popup.PopupPredicted(message, null, uid, user.Value, PopupType.Small);
            _adminLogger.Add(LogType.Action, LogImpact.Low, ref adminMessage);
        }

        loadedDie.SelectedSide = selectedSide;

        UpdateUi((uid, die, loadedDie));
        Dirty(uid, loadedDie);
    }
}
