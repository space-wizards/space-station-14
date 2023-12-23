using Content.Shared.Interaction.Events;
using Content.Shared.Toggleable;
using Content.Shared.Temperature;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;

namespace Content.Shared.Item.ItemToggle;
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
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemToggleComponent, ItemUnwieldedEvent>(TurnOffonUnwielded);
        SubscribeLocalEvent<ItemToggleComponent, ItemWieldedEvent>(TurnOnonWielded);
        SubscribeLocalEvent<ItemToggleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ItemToggleHotComponent, IsHotEvent>(OnIsHotEvent);
        SubscribeLocalEvent<ItemToggleActiveSoundComponent, ItemToggleActiveSoundUpdateEvent>(UpdateActiveSound);
        SubscribeLocalEvent<AppearanceComponent, ItemToggleAppearanceUpdateEvent>(UpdateAppearance);
        SubscribeLocalEvent<ItemToggleComponent, ItemToggleLightUpdateEvent>(UpdateLight);
        SubscribeLocalEvent<ItemToggleComponent, ItemTogglePlayToggleSoundEvent>(PlayToggleSound);
        SubscribeLocalEvent<ItemToggleComponent, ItemTogglePlayFailSoundEvent>(PlayFailToggleSound);
    }

    private void OnUseInHand(EntityUid uid, ItemToggleComponent itemToggle, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        Toggle(uid, args.User, predicted: itemToggle.Predictable, itemToggle: itemToggle);
    }

    /// <summary>
    /// Used when an item is attempted to be toggled. 
    /// </summary>
    public void Toggle(EntityUid uid, EntityUid? user = null, bool predicted = true, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return;

        if (itemToggle.Activated)
        {
            TryDeactivate(uid, user, itemToggle: itemToggle, predicted: predicted);
        }
        else
        {
            TryActivate(uid, user, itemToggle: itemToggle, predicted: predicted);
        }
    }

    /// <summary>
    /// Used when an item is attempting to be activated. It returns false if the attempt fails any reason, interrupting the activation.
    /// </summary>
    public bool TryActivate(EntityUid uid, EntityUid? user = null, bool predicted = true, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return false;

        if (itemToggle.Activated)
            return true;

        var attempt = new ItemToggleActivateAttemptEvent(user);
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
        {
            //Raises the event to play the failure to activate noise.
            var evPlayFailToggleSound = new ItemTogglePlayFailSoundEvent(Predicted: predicted, user);
            RaiseLocalEvent(uid, ref evPlayFailToggleSound);

            return false;
        }
        // If the item's toggle is unpredictable because of something like requiring fuel or charge, then clients exit here.
        // Otherwise you get stuff like an item activating client-side and then turning back off when it synchronizes with the server.
        if (predicted == false && _netManager.IsClient)
            return true;

        Activate(uid, itemToggle);

        var evPlayToggleSound = new ItemTogglePlayToggleSoundEvent(Activated: true, Predicted: predicted, user);
        RaiseLocalEvent(uid, ref evPlayToggleSound);

        var evActiveSound = new ItemToggleActiveSoundUpdateEvent(Activated: true, Predicted: predicted, user);
        RaiseLocalEvent(uid, ref evActiveSound);

        var toggleUsed = new ItemToggleDoneEvent(Activated: true, user);
        RaiseLocalEvent(uid, ref toggleUsed);

        return true;
    }

    /// <summary>
    /// Used when an item is attempting to be deactivated. It returns false if the attempt fails any reason, interrupting the deactivation.
    /// </summary>
    public bool TryDeactivate(EntityUid uid, EntityUid? user = null, bool predicted = true, ItemToggleComponent? itemToggle = null)
    {
        if (!Resolve(uid, ref itemToggle))
            return false;

        if (!itemToggle.Activated)
            return true;

        var attempt = new ItemToggleDeactivateAttemptEvent(user);
        RaiseLocalEvent(uid, ref attempt);

        if (attempt.Cancelled)
        {
            return false;
        }

        // If the item's toggle is unpredictable because of something like requiring fuel or charge, then clients exit here.
        if (predicted == false && _netManager.IsClient)
            return true;

        Deactivate(uid, itemToggle);

        var evPlayToggleSound = new ItemTogglePlayToggleSoundEvent(Activated: false, Predicted: predicted, user);
        RaiseLocalEvent(uid, ref evPlayToggleSound);

        var evActiveSound = new ItemToggleActiveSoundUpdateEvent(Activated: false, Predicted: predicted, user);
        RaiseLocalEvent(uid, ref evActiveSound);

        var toggleUsed = new ItemToggleDoneEvent(Activated: false, user);
        RaiseLocalEvent(uid, ref toggleUsed);

        return true;
    }

    /// <summary>
    /// Used to make the actual changes to the item's components on activation.
    /// </summary>
    private void Activate(EntityUid uid, ItemToggleComponent itemToggle)
    {
        UpdateComponents(uid, itemToggle.Activated = true);

        Dirty(uid, itemToggle);
    }

    /// <summary>
    /// Used to make the actual changes to the item's components on deactivation.
    /// </summary>
    private void Deactivate(EntityUid uid, ItemToggleComponent itemToggle)
    {
        UpdateComponents(uid, itemToggle.Activated = false);

        Dirty(uid, itemToggle);
    }

    /// <summary>
    /// Used to raise events to update components on toggle.
    /// </summary>
    private void UpdateComponents(EntityUid uid, bool activated)
    {
        var evSize = new ItemToggleSizeUpdateEvent(activated);
        RaiseLocalEvent(uid, ref evSize);

        var evMelee = new ItemToggleMeleeWeaponUpdateEvent(activated);
        RaiseLocalEvent(uid, ref evMelee);

        var evAppearance = new ItemToggleAppearanceUpdateEvent(activated);
        RaiseLocalEvent(uid, ref evAppearance);

        var evLight = new ItemToggleLightUpdateEvent(activated);
        RaiseLocalEvent(uid, ref evLight);

        var evReflect = new ItemToggleReflectUpdateEvent(activated);
        RaiseLocalEvent(uid, ref evReflect);
    }

    /// <summary>
    /// Used for items that require to be wielded in both hands to activate. For instance the dual energy sword will turn off if not wielded.
    /// </summary>
    private void TurnOffonUnwielded(EntityUid uid, ItemToggleComponent itemToggle, ItemUnwieldedEvent args)
    {
        if (itemToggle.Activated)
            TryDeactivate(uid, args.User, itemToggle: itemToggle);
    }

    /// <summary>
    /// Wieldable items will automatically turn on when wielded.
    /// </summary>
    private void TurnOnonWielded(EntityUid uid, ItemToggleComponent itemToggle, ref ItemWieldedEvent args)
    {
        if (!itemToggle.Activated)
            TryActivate(uid, itemToggle: itemToggle);
    }

    public bool IsActivated(EntityUid uid, ItemToggleComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return true; // assume always activated if no component

        return comp.Activated;
    }

    /// <summary>
    /// Used to make the item hot when activated.
    /// </summary>
    private void OnIsHotEvent(EntityUid uid, ItemToggleHotComponent itemToggleHot, IsHotEvent args)
    {
        if (itemToggleHot.IsHotWhenActivated)
            args.IsHot = IsActivated(uid);
    }

    /// <summary>
    /// Used to update item appearance.
    /// </summary>
    private void UpdateAppearance(EntityUid uid, AppearanceComponent appearance, ref ItemToggleAppearanceUpdateEvent args)
    {
        _appearance.SetData(uid, ToggleableLightVisuals.Enabled, args.Activated, appearance);
        _appearance.SetData(uid, ToggleVisuals.Toggled, args.Activated, appearance);
    }

    /// <summary>
    /// Used to update light settings.
    /// </summary>
    private void UpdateLight(EntityUid uid, ItemToggleComponent comp, ref ItemToggleLightUpdateEvent args)
    {
        if (!_light.TryGetLight(uid, out var light))
            return;

        _light.SetEnabled(uid, args.Activated, light);
    }

    /// <summary>
    /// Used to update the looping active sound linked to the entity.
    /// </summary>
    private void UpdateActiveSound(EntityUid uid, ItemToggleActiveSoundComponent activeSound, ref ItemToggleActiveSoundUpdateEvent args)
    {
        if (args.Activated)
        {
            if (activeSound.ActiveSound != null && activeSound.PlayingStream == null)
            {
                if (args.Predicted)
                    activeSound.PlayingStream = _audio.PlayPredicted(activeSound.ActiveSound, uid, args.User, AudioParams.Default.WithLoop(true)).Value.Entity;
                else
                    activeSound.PlayingStream = _audio.PlayPvs(activeSound.ActiveSound, uid, AudioParams.Default.WithLoop(true)).Value.Entity;
            }
        }
        else
        {
            activeSound.PlayingStream = _audio.Stop(activeSound.PlayingStream);
        }
    }

    /// <summary>
    /// Used to play a toggle sound.
    /// </summary>
    private void PlayToggleSound(EntityUid uid, ItemToggleComponent itemToggle, ref ItemTogglePlayToggleSoundEvent args)
    {
        SoundSpecifier? soundToPlay;
        if (args.Activated)
            soundToPlay = itemToggle.SoundActivate;
        else
            soundToPlay = itemToggle.SoundDeactivate;

        if (soundToPlay == null)
            return;

        if (args.Predicted)
            _audio.PlayPredicted(soundToPlay, uid, args.User);
        else
            _audio.PlayPvs(soundToPlay, uid);
    }

    /// <summary>
    /// Used to play a failure to toggle sound.
    /// </summary>
    private void PlayFailToggleSound(EntityUid uid, ItemToggleComponent itemToggle, ref ItemTogglePlayFailSoundEvent args)
    {
        if (args.Predicted)
            _audio.PlayPredicted(itemToggle.SoundFailToActivate, uid, args.User);
        else
            _audio.PlayPvs(itemToggle.SoundFailToActivate, uid);
    }
}
