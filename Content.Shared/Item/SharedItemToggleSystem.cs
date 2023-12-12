using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Temperature;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

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

        itemToggle.User = args.User;

        if (TryComp<WieldableComponent>(uid, out var wieldableComp))
            return;

        Toggle(uid, itemToggle.User, itemToggle);
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

        Toggle(uid, itemToggle.User, itemToggle);
    }

    public bool TryActivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return false;

        if (itemToggle.Activated)
            return true;

        var attempt = new ItemToggleActivateAttemptEvent();
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
        {
            //Play the failure to activate noise if there is any.
            _audio.PlayPredicted(itemToggle.FailToActivateSound, uid, user);
            return false;
        }

        Activate(uid, itemToggle);

        var ev = new ItemToggleActivatedEvent();
        RaiseLocalEvent(uid, ref ev);

        _audio.PlayPredicted(itemToggle.ActivateSound, uid, user);
        //Starts the active sound (like humming).
        itemToggle.Stream = _audio.PlayPredicted(itemToggle.ActiveSound, uid, user, AudioParams.Default.WithLoop(true))?.Entity;

        return true;
    }

    public bool TryDeactivate(EntityUid uid, EntityUid? user = null, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return false;

        if (!itemToggle.Activated)
            return true;

        var attempt = new ItemToggleDeactivateAttemptEvent();
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
            return false;

        Deactivate(uid, itemToggle);

        var ev = new ItemToggleDeactivatedEvent();
        RaiseLocalEvent(uid, ref ev);

        _audio.PlayPredicted(itemToggle.DeactivateSound, uid, user);
        //Stops the active sound (like humming).
        itemToggle.Stream = _audio.Stop(itemToggle.Stream);

        return true;
    }

    //Makes the actual changes to the item.
    private void Activate(EntityUid uid, ItemToggleComponent itemToggle)
    {
        itemToggle.Activated = true;

        UpdateItemComponent(uid, itemToggle);
        UpdateWeaponComponent(uid, itemToggle);
        UpdateAppearance(uid, itemToggle);
        UpdateLight(uid, itemToggle);

        var ev = new ItemToggleActivatedServerChangesEvent();
        RaiseLocalEvent(uid, ref ev);

        Dirty(uid, itemToggle);
    }

    private void Deactivate(EntityUid uid, ItemToggleComponent itemToggle)
    {
        itemToggle.Activated = false;

        UpdateItemComponent(uid, itemToggle);
        UpdateWeaponComponent(uid, itemToggle);
        UpdateAppearance(uid, itemToggle);
        UpdateLight(uid, itemToggle);

        var ev = new ItemToggleDeactivatedServerChangesEvent();
        RaiseLocalEvent(uid, ref ev);

        Dirty(uid, itemToggle);
    }


    private void TurnOffonUnwielded(EntityUid uid, ItemToggleComponent itemToggle, ItemUnwieldedEvent args)
    {
        if (itemToggle.Activated)
            TryDeactivate(uid, itemToggle: itemToggle);
    }

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
