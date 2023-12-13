using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Temperature;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;

namespace Content.Shared.Item;
/// <summary>
/// Handles generic item toggles, like a welder turning on and off, or an e-sword.
/// </summary>
/// <remarks>
/// If you need extended functionality (e.g. requiring power) then add a new component and use events.
/// </remarks>
public abstract class SharedItemToggleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ItemToggleComponent, IsHotEvent>(OnIsHotEvent);
        SubscribeLocalEvent<ItemToggleComponent, ItemUnwieldedEvent>(TurnOffonUnwielded);
        SubscribeLocalEvent<ItemToggleComponent, ItemWieldedEvent>(TurnOnonWielded);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleForceToggleEvent>(ForceToggle);
    }
    private void OnUseInHand(EntityUid uid, ItemToggleComponent itemToggle, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp<WieldableComponent>(uid, out var wieldableComp))
            return;

        Toggle(uid, args.User, itemToggle);
    }

    public void Toggle(EntityUid uid, EntityUid? user = null, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return;

        if (itemToggle.Activated)
        {
            TryDeactivate(uid, user, itemToggle: itemToggle);
        }
        else
        {
            TryActivate(uid, user, itemToggle: itemToggle);
        }
    }

    public void ForceToggle(EntityUid uid, ItemToggleComponent itemToggle, ref ItemToggleForceToggleEvent args)
    {
        Toggle(uid, args.User, itemToggle);
    }

    public bool TryActivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return false;

        if (itemToggle.Activated)
            return true;

        //The client cannot predict if the attempt to turn on fails or not since the battery and fuel systems are server side (for now). Potential future TODO
        if (_netManager.IsServer)
        {
            var attempt = new ItemToggleActivateAttemptEvent(user);
            RaiseLocalEvent(uid, ref attempt);

            if (attempt.Cancelled)
            {
                //Play the failure to activate noise if there is any.
                _audio.PlayPvs(itemToggle.SoundFailToActivate, uid);
                return false;
            }

            // At this point the server knows that the activation went through successfully, so we play the sounds and make the changes.
            _audio.PlayPvs(itemToggle.SoundActivate, uid);
            //Starts the active sound (like humming).
            if (itemToggle.ActiveSound != null && itemToggle.PlayingStream == null)
            {
                itemToggle.PlayingStream = _audio.PlayPvs(itemToggle.ActiveSound, uid, AudioParams.Default.WithLoop(true)).Value.Entity;
            }

            Activate(uid, itemToggle);
            var ev = new ItemToggleActivatedEvent();
            RaiseLocalEvent(uid, ref ev);
        }
        return true;
    }

    public bool TryDeactivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return false;

        if (!itemToggle.Activated)
            return true;

        //Since there is currently no system that cancels a deactivation, it's all predicted.
        var attempt = new ItemToggleDeactivateAttemptEvent(user);
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled && uid.Id == GetNetEntity(uid).Id)
        {
            return false;
        }
        else
        {
            _audio.PlayPredicted(itemToggle.SoundDeactivate, uid, user);
            itemToggle.PlayingStream = _audio.Stop(itemToggle.PlayingStream);

            Deactivate(uid, itemToggle);

            var ev = new ItemToggleDeactivatedEvent();
            RaiseLocalEvent(uid, ref ev);

            return true;
        }
    }

    //Makes the actual changes to the item's components on activation.
    private void Activate(EntityUid uid, ItemToggleComponent itemToggle)
    {
        itemToggle.Activated = true;

        UpdateItemComponent(uid, itemToggle);
        UpdateWeaponComponent(uid, itemToggle);
        UpdateAppearance(uid, itemToggle);
        UpdateLight(uid, itemToggle);

        Dirty(uid, itemToggle);
    }
    //Makes the actual changes to the item's components on deactivation.
    private void Deactivate(EntityUid uid, ItemToggleComponent itemToggle)
    {
        itemToggle.Activated = false;

        UpdateItemComponent(uid, itemToggle);
        UpdateWeaponComponent(uid, itemToggle);
        UpdateAppearance(uid, itemToggle);
        UpdateLight(uid, itemToggle);

        Dirty(uid, itemToggle);
    }

    /// <summary>
    /// Used for items that require to be wielded in both hands to activate. For instance the dual energy sword will turn off if not wielded.
    /// </summary>
    private void TurnOffonUnwielded(EntityUid uid, ItemToggleComponent itemToggle, ItemUnwieldedEvent args)
    {
        if (itemToggle.Activated)
            TryDeactivate(uid, itemToggle: itemToggle);
    }

    /// <summary>
    /// Wieldable items will automatically turn on when wielded.
    /// </summary>
    private void TurnOnonWielded(EntityUid uid, ItemToggleComponent itemToggle, ref ItemWieldedEvent args)
    {
        if (!itemToggle.Activated)
            TryActivate(uid, itemToggle: itemToggle);
    }


    /// <summary>
    /// Used to update item appearance.
    /// </summary>
    private void UpdateAppearance(EntityUid uid, ItemToggleComponent itemToggle)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, itemToggle.Activated, appearance);
        _appearance.SetData(uid, ToggleVisuals.Toggled, itemToggle.Activated, appearance);
    }

    /// <summary>
    /// Used to update light settings.
    /// </summary>
    private void UpdateLight(EntityUid uid, ItemToggleComponent itemToggle)
    {
        if (!_light.TryGetLight(uid, out var light))
            return;

        _light.SetEnabled(uid, itemToggle.Activated, light);
    }

    /// <summary>
    /// Used to update weapon component aspects, like hit sounds and damage values.
    /// </summary>
    private void UpdateWeaponComponent(EntityUid uid, ItemToggleComponent itemToggle)
    {
        if (!TryComp(uid, out MeleeWeaponComponent? meleeWeapon))
            return;

        //Sets the value for deactivated damage to the item's default if none is stated.
        itemToggle.DeactivatedDamage ??= meleeWeapon.Damage;

        if (itemToggle.Activated)
        {
            meleeWeapon.Damage = itemToggle.ActivatedDamage;
            meleeWeapon.HitSound = itemToggle.ActivatedSoundOnHit;

            if (itemToggle.ActivatedSoundOnSwing != null)
                meleeWeapon.SwingSound = itemToggle.ActivatedSoundOnSwing;

            if (itemToggle.DeactivatedSecret)
                meleeWeapon.Hidden = false;
        }
        else
        {
            meleeWeapon.Damage = itemToggle.DeactivatedDamage;
            meleeWeapon.HitSound = itemToggle.DeactivatedSoundOnHit;

            if (itemToggle.DeactivatedSoundOnSwing != null)
                meleeWeapon.SwingSound = itemToggle.DeactivatedSoundOnSwing;

            if (itemToggle.DeactivatedSecret)
                meleeWeapon.Hidden = true;
        }

        Dirty(uid, meleeWeapon);
    }

    /// <summary>
    /// Used to update item component aspects, like size values for items that expand when activated (heh).
    /// </summary>
    private void UpdateItemComponent(EntityUid uid, ItemToggleComponent itemToggle)
    {
        if (!TryComp(uid, out ItemComponent? item))
            return;

        //Sets the deactivated size to the default if none is stated.
        itemToggle.DeactivatedSize ??= item.Size;

        if (itemToggle.Activated)
            _item.SetSize(uid, itemToggle.ActivatedSize, item);
        else
            _item.SetSize(uid, (ProtoId<ItemSizePrototype>) itemToggle.DeactivatedSize, item);

        Dirty(uid, item);
    }

    /// <summary>
    /// Used to make the item hot when activated.
    /// </summary>
    private void OnIsHotEvent(EntityUid uid, ItemToggleComponent itemToggle, IsHotEvent args)
    {
        if (itemToggle.IsHotWhenActivated)
            args.IsHot = itemToggle.Activated;
    }
}
