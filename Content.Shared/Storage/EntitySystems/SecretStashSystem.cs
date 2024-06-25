using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Destructible;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.Examine;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Verbs;
using Content.Shared.IdentityManagement;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
///     Secret Stash allows an item to be hidden within.
/// </summary>
public sealed class SecretStashSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SecretStashComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SecretStashComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<SecretStashComponent, SecretStashPryDoAfterEventToggleIsOpen>(OnSecretStashOpenStateToggled);
        SubscribeLocalEvent<SecretStashComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SecretStashComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SecretStashComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SecretStashComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerb);
    }

    private void OnInit(Entity<SecretStashComponent> entity, ref ComponentInit args)
    {
        UpdateAppearance(entity);
        entity.Comp.ItemContainer = _containerSystem.EnsureContainer<ContainerSlot>(entity, "stash", out _);
        Dirty(entity);
    }

    private void OnDestroyed(Entity<SecretStashComponent> entity, ref DestructionEventArgs args)
    {
        var storedInside = _containerSystem.EmptyContainer(entity.Comp.ItemContainer);
        if (storedInside != null && storedInside.Count >= 1)
        {
            var popup = Loc.GetString("comp-secret-stash-on-destroyed-popup", ("stashname", GetStashName(entity)));
            _popupSystem.PopupEntity(popup, storedInside[0], PopupType.MediumCaution);
        }
    }

    private void OnInteractUsing(Entity<SecretStashComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (entity.Comp.IsStashOpen)
        {
            if (!entity.Comp.CanBeOpenedAndClosed || !TryOpenOrCloseStash(entity, args.Used, args.User))
                TryStashItem(entity, args.User, args.Used);

            args.Handled = true;
        }
        else if (entity.Comp.CanBeOpenedAndClosed)
            args.Handled = TryOpenOrCloseStash(entity, args.Used, args.User);
    }

    private void OnInteractHand(Entity<SecretStashComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (entity.Comp.IsStashOpen)
            args.Handled = TryGetItem(entity, args.User);
    }

    /// <summary>
    ///     Try to open or close the given stash.
    /// </summary>
    /// <returns>Returns false if you can't interact with the stash with the given item.</returns>
    private bool TryOpenOrCloseStash(Entity<SecretStashComponent> entity, EntityUid? toolToToggle, EntityUid user)
    {
        var neededToolQuantity = entity.Comp.IsStashOpen ? entity.Comp.StashCloseToolQualityNeeded : entity.Comp.StashOpenToolQualityNeeded;
        var time = entity.Comp.IsStashOpen ? entity.Comp.CloseStashTime : entity.Comp.OpenStashTime;
        var evt = new SecretStashPryDoAfterEventToggleIsOpen();

        // If neededToolQuantity is null it can only be open be opened with the verbs.
        if (toolToToggle == null || neededToolQuantity == null)
            return false;

        return _tool.UseTool(toolToToggle.Value, user, entity, time, neededToolQuantity, evt);
    }

    private void OnSecretStashOpenStateToggled(Entity<SecretStashComponent> entity, ref SecretStashPryDoAfterEventToggleIsOpen args)
    {
        if (args.Cancelled)
            return;

        ToggleSecretStashState(entity);
    }
    /// <summary>
    ///     Toggle the state of the stash and update appearance.
    /// </summary>
    private void ToggleSecretStashState(Entity<SecretStashComponent> entity)
    {
        entity.Comp.IsStashOpen = !entity.Comp.IsStashOpen;
        UpdateAppearance(entity);
        Dirty(entity);
    }

    /// <summary>
    ///     Tries to hide the given item into the stash.
    /// </summary>
    /// <returns>True if item was hidden inside stash and false otherwise.</returns>
    private bool TryStashItem(Entity<SecretStashComponent> entity, EntityUid userUid, EntityUid itemToHideUid)
    {
        if (!TryComp<ItemComponent>(itemToHideUid, out var itemComp))
            return false;

        _audio.PlayPredicted(entity.Comp.TryInsertItemSound, entity, userUid, AudioParams.Default.WithVariation(0.25f));

        // check if secret stash is already occupied
        var container = entity.Comp.ItemContainer;
        if (HasItemInside(entity))
        {
            var popup = Loc.GetString("comp-secret-stash-action-hide-container-not-empty");
            _popupSystem.PopupClient(popup, entity, userUid);
            return false;
        }

        // check if item is too big to fit into secret stash
        if (_item.GetSizePrototype(itemComp.Size) > _item.GetSizePrototype(entity.Comp.MaxItemSize))
        {
            var msg = Loc.GetString("comp-secret-stash-action-hide-item-too-big",
                ("item", itemToHideUid), ("stashname", GetStashName(entity)));
            _popupSystem.PopupClient(msg, entity, userUid);
            return false;
        }

        // try to move item from hands to stash container
        if (!_handsSystem.TryDropIntoContainer(userUid, itemToHideUid, container))
            return false;

        // all done, show success message
        var successMsg = Loc.GetString("comp-secret-stash-action-hide-success",
            ("item", itemToHideUid), ("stashname", GetStashName(entity)));
        _popupSystem.PopupClient(successMsg, entity, userUid);
        return true;
    }

    /// <summary>
    ///     Try the given item in the stash and place it in users hand.
    ///     If user can't take hold the item in their hands, the item will be dropped onto the ground.
    /// </summary>
    /// <returns>True if user received item.</returns>
    private bool TryGetItem(Entity<SecretStashComponent> entity, EntityUid userUid)
    {
        if (!TryComp<HandsComponent>(userUid, out var handsComp))
            return false;

        _audio.PlayPredicted(entity.Comp.TryRemoveItemSound, entity, userUid, AudioParams.Default.WithVariation(0.25f));

        // check if secret stash has something inside
        var itemInStash = entity.Comp.ItemContainer.ContainedEntity;
        if (itemInStash == null)
            return false;

        _handsSystem.PickupOrDrop(userUid, itemInStash.Value, handsComp: handsComp);

        // show success message
        var successMsg = Loc.GetString("comp-secret-stash-action-get-item-found-something",
            ("stashname", GetStashName(entity)));
        _popupSystem.PopupClient(successMsg, entity, userUid);

        return true;
    }

    #region Helper functions

    private string GetStashName(Entity<SecretStashComponent> entity)
    {
        if (entity.Comp.SecretStashName == null)
            return Identity.Name(entity, EntityManager);
        return Loc.GetString(entity.Comp.SecretStashName);
    }

    private void UpdateAppearance(Entity<SecretStashComponent> entity)
    {
        _appearance.SetData(entity, StashVisuals.StashVisualState, entity.Comp.IsStashOpen ? StashVisualState.StashOpen : StashVisualState.StashClosed);
    }

    private bool HasItemInside(Entity<SecretStashComponent> entity)
    {
        return entity.Comp.ItemContainer.ContainedEntity != null;
    }

    #endregion

    #region User interface functions

    private void OnExamine(Entity<SecretStashComponent> entity, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange && entity.Comp.IsStashOpen)
        {
            if (HasItemInside(entity))
            {
                var msg = Loc.GetString("comp-secret-stash-on-examine-found-hidden-item", ("stashname", GetStashName(entity)));
                args.PushMarkup(msg);
            }
        }
    }

    private void OnGetVerb(Entity<SecretStashComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !entity.Comp.HasVerbs)
            return;

        var user = args.User;
        var item = args.Using;
        var stashName = GetStashName(entity);

        var itemVerb = new InteractionVerb();
        var toggleVerb = new InteractionVerb();

        // This will add the verb relating to inserting / grabbing items.
        if (entity.Comp.IsStashOpen)
        {
            if (item != null)
            {
                itemVerb.Text = Loc.GetString("comp-secret-stash-verb-insert-into-stash");
                if (HasItemInside(entity))
                {
                    itemVerb.Disabled = true;
                    itemVerb.Message = Loc.GetString("comp-secret-stash-verb-insert-message-item-already-inside", ("stashname", stashName));
                }
                else
                {
                    itemVerb.Message = Loc.GetString("comp-secret-stash-verb-insert-message-no-item", ("item", item), ("stashname", stashName));
                }

                itemVerb.Act = () => TryStashItem(entity, user, item.Value);
            }
            else
            {
                itemVerb.Text = Loc.GetString("comp-secret-stash-verb-take-out-item");
                itemVerb.Message = Loc.GetString("comp-secret-stash-verb-take-out-message-something", ("stashname", stashName));
                if (!HasItemInside(entity))
                {
                    itemVerb.Disabled = true;
                    itemVerb.Message = Loc.GetString("comp-secret-stash-verb-take-out-message-nothing", ("stashname", stashName));
                }

                itemVerb.Act = () => TryGetItem(entity, user);
            }

            args.Verbs.Add(itemVerb);
        }

        // You can't open or close so skip this verb.
        if (!entity.Comp.CanBeOpenedAndClosed)
            return;
        toggleVerb.IconEntity = GetNetEntity(item);
        // This verb is for opening / closing the stash.
        if (entity.Comp.IsStashOpen)
        {
            toggleVerb.Text = toggleVerb.Message = Loc.GetString("comp-secret-stash-verb-close");
            var neededQual = entity.Comp.StashCloseToolQualityNeeded;

            // If neededQual is null you don't need a tool to open / close.
            if (neededQual != null &&
                (item == null || !_tool.HasQuality(item.Value, neededQual)))
            {
                toggleVerb.Disabled = true;
                toggleVerb.Message = Loc.GetString("comp-secret-stash-verb-cant-close", ("stashname", stashName));
            }

            if (neededQual == null)
                toggleVerb.Act = () => ToggleSecretStashState(entity);
            else
                toggleVerb.Act = () => TryOpenOrCloseStash(entity, item, user);

            args.Verbs.Add(toggleVerb);
        }
        else
        {
            // The open verb should only appear when holding the correct tool or if no tool is needed.

            toggleVerb.Text = toggleVerb.Message = Loc.GetString("comp-secret-stash-verb-open");
            var neededQual = entity.Comp.StashOpenToolQualityNeeded;

            if (neededQual == null)
            {
                toggleVerb.Act = () => ToggleSecretStashState(entity);
                args.Verbs.Add(toggleVerb);
            }
            else if (item != null && _tool.HasQuality(item.Value, neededQual))
            {
                toggleVerb.Act = () => TryOpenOrCloseStash(entity, item, user);
                args.Verbs.Add(toggleVerb);
            }
        }
    }

    #endregion
}
