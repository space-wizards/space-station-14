using Content.Shared.Construction.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Materials;
using Content.Shared.Nutrition;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;

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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ToolOpenableSystem _toolOpenableSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SecretStashComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SecretStashComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<SecretStashComponent, GotReclaimedEvent>(OnReclaimed);
        SubscribeLocalEvent<SecretStashComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(ToolOpenableSystem), typeof(AnchorableSystem) });
        SubscribeLocalEvent<SecretStashComponent, AfterFullyEatenEvent>(OnEaten);
        SubscribeLocalEvent<SecretStashComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SecretStashComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerb);
    }

    private void OnInit(Entity<SecretStashComponent> entity, ref ComponentInit args)
    {
        entity.Comp.ItemContainer = _containerSystem.EnsureContainer<ContainerSlot>(entity, "stash", out _);
    }

    private void OnDestroyed(Entity<SecretStashComponent> entity, ref DestructionEventArgs args)
    {
        DropContentsAndAlert(entity);
    }

    private void OnReclaimed(Entity<SecretStashComponent> entity, ref GotReclaimedEvent args)
    {
        DropContentsAndAlert(entity, args.ReclaimerCoordinates);
    }

    private void OnEaten(Entity<SecretStashComponent> entity, ref AfterFullyEatenEvent args)
    {
        // TODO: When newmed is finished should do damage to teeth (Or something like that!)
        var damage = entity.Comp.DamageEatenItemInside;
        if (HasItemInside(entity) && damage != null)
            _damageableSystem.TryChangeDamage(args.User, damage, true);
    }

    private void OnInteractUsing(Entity<SecretStashComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsStashOpen(entity))
            return;

        args.Handled = TryStashItem(entity, args.User, args.Used);
    }

    private void OnInteractHand(Entity<SecretStashComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled || !IsStashOpen(entity))
            return;

        args.Handled = TryGetItem(entity, args.User);
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

        // check if item is too big to fit into secret stash or is in the blacklist
        if (_item.GetSizePrototype(itemComp.Size) > _item.GetSizePrototype(entity.Comp.MaxItemSize) ||
            _whitelistSystem.IsBlacklistPass(entity.Comp.Blacklist, itemToHideUid))
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

    private void OnGetVerb(Entity<SecretStashComponent> entity, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !entity.Comp.HasVerbs)
            return;

        var user = args.User;
        var item = args.Using;
        var stashName = GetStashName(entity);

        var itemVerb = new InteractionVerb();

        // This will add the verb relating to inserting / grabbing items.
        if (IsStashOpen(entity))
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
    }

    #region Helper functions

    /// <returns>
    ///     The stash name if it exists, or the entity name if it doesn't.
    ///  </returns>
    private string GetStashName(Entity<SecretStashComponent> entity)
    {
        if (entity.Comp.SecretStashName == null)
            return Identity.Name(entity, EntityManager);
        return Loc.GetString(entity.Comp.SecretStashName);
    }

    /// <returns>
    ///     True if the stash is open OR the there is no toolOpenableComponent attacheded to the entity
    ///     and false otherwise.
    ///  </returns>
    private bool IsStashOpen(Entity<SecretStashComponent> stash)
    {
        return _toolOpenableSystem.IsOpen(stash);
    }

    private bool HasItemInside(Entity<SecretStashComponent> entity)
    {
        return entity.Comp.ItemContainer.ContainedEntity != null;
    }

    /// <summary>
    ///     Drop the item stored in the stash and alert all nearby players with a popup.
    /// </summary>
    private void DropContentsAndAlert(Entity<SecretStashComponent> entity, EntityCoordinates? cords = null)
    {
        var storedInside = _containerSystem.EmptyContainer(entity.Comp.ItemContainer, true, cords);
        if (storedInside != null && storedInside.Count >= 1)
        {
            var popup = Loc.GetString("comp-secret-stash-on-destroyed-popup", ("stashname", GetStashName(entity)));
            _popupSystem.PopupPredicted(popup, storedInside[0], null, PopupType.MediumCaution);
        }
    }

    #endregion
}
