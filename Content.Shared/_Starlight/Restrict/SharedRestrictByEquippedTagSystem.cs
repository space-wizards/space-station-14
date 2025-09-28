using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared._Starlight.Restrict;

/// <summary>
/// Shared system that handles restricting items based on whether you have required item equipped.
/// </summary>
public abstract partial class SharedRestrictByEquippedTagSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RestrictByEquippedTagComponent, InteractionAttemptEvent>(OnAttemptInteract);
        SubscribeLocalEvent<RestrictByEquippedTagComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<RestrictByEquippedTagComponent, AttemptShootEvent>(OnShotAttempt);
    }

    /// <summary>
    /// Checks if a user has an equipped item with the required tag.
    /// Only checks for actually equipped items, not items in hands or pockets.
    /// </summary>
    protected bool HasRequiredEquippedItem(EntityUid user, RestrictByEquippedTagComponent component)
    {
        if (!Exists(user))
            return false;

        // Get all equipped items (excluding pockets and hands)
        var equippedItems = _inventorySystem.GetHandOrInventoryEntities(
            (user, null, null), 
            SlotFlags.WITHOUT_POCKET);

        // Check if any equipped item has the required tag
        foreach (var item in equippedItems)
        {
            if (Exists(item) && _tagSystem.HasTag(item, component.RequiredTag))
                return true;
        }

        return false;
    }

    protected virtual void PopupClient(string message, EntityUid user)
    {
        _popup.PopupClient(message, user);
    }

    protected virtual void PlayDenialSound(SoundSpecifier? sound, EntityUid entity)
    {
        if (sound != null)
            _audio.PlayGlobal(sound, Filter.Pvs(entity), false);
    }

    private void OnAttemptInteract(Entity<RestrictByEquippedTagComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!Exists(ent) || !Exists(args.Uid))
            return;

        if (HasRequiredEquippedItem(args.Uid, ent.Comp))
            return;

        args.Cancelled = true;
        PopupClient(Loc.GetString(ent.Comp.DenialMessage), args.Uid);
        PlayDenialSound(ent.Comp.DenialSound, ent);
    }
    
    private void OnShotAttempt(Entity<RestrictByEquippedTagComponent> ent, ref AttemptShootEvent args)
    {
        if (!Exists(ent) || !Exists(args.User))
            return;

        if (HasRequiredEquippedItem(args.User, ent.Comp))
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString(ent.Comp.DenialMessage);
        PlayDenialSound(ent.Comp.DenialSound, ent);
    }

    private void OnAttemptMelee(Entity<RestrictByEquippedTagComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!Exists(ent) || !Exists(args.User))
            return;

        if (HasRequiredEquippedItem(args.User, ent.Comp))
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString(ent.Comp.DenialMessage);
        PlayDenialSound(ent.Comp.DenialSound, ent);
    }
} 