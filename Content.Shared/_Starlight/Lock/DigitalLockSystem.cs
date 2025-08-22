using Content.Shared.Lock;
using Content.Shared.Audio;
using Content.Shared.Tools.Systems;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Content.Shared.Tools.Components;
using System.Linq;
using Content.Shared.Electrocution;
using Content.Shared.Examine;
using Robust.Shared.Utility;
using Content.Shared.Atmos.Piping.Components;

namespace Content.Shared._Starlight.Lock;

public sealed class DigitalLockSystem : EntitySystem
{
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Hacking
        SubscribeLocalEvent<DigitalLockComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockMaintenanceOpenDoAfterEvent>(OnMaintenanceOpen);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockResetDoAfterEvent>(OnReset);
        SubscribeLocalEvent<DigitalLockComponent, ExaminedEvent>(OnExamine);

        // UI
        SubscribeLocalEvent<DigitalLockComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<DigitalLockComponent, DigitalLockKeypadMessage>(OnKeypadButtonPressed);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockKeypadClearMessage>(OnClearButtonPressed);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockKeypadEnterMessage>(OnEnterButtonPressed);
    }

    #region Hacking

    /// <summary>
    /// Try To open maintenance panel/Reset password when interact with tool
    /// </summary>
    private void OnInteractUsing(EntityUid uid, DigitalLockComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_tool.HasQuality(args.Used, component.OpenQuality))
            args.Handled = _tool.UseTool(args.Used, args.User, uid, 2f, component.OpenQuality, new DigitalLockMaintenanceOpenDoAfterEvent());
        else if (_tool.HasQuality(args.Used, component.ResetQuality) && component.MaintenanceOpen && component.Code != "")
        {
            var codeLength = component.Code.Length;
            _appearance.SetData(uid, DigitalLockVisuals.Spark, true);
            _ambient.SetAmbience(uid, true);
            args.Handled = _electrocution.TryDoElectrocution(args.User, uid, 5, TimeSpan.FromSeconds(2), true) || _tool.UseTool(args.Used, args.User, uid, 4f * codeLength, component.ResetQuality, new DigitalLockResetDoAfterEvent());
        }

        args.Handled = false;
    }

    /// <summary>
    /// Doafter for opening maintenance panel
    /// </summary>
    private void OnMaintenanceOpen(EntityUid uid, DigitalLockComponent component, DigitalLockMaintenanceOpenDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        component.MaintenanceOpen = !component.MaintenanceOpen;
        args.Handled = true;
    }

    /// <summary>
    /// Doafter for resetting password
    /// </summary>
    private void OnReset(EntityUid uid, DigitalLockComponent component, DigitalLockResetDoAfterEvent args)
    {
        _appearance.SetData(uid, DigitalLockVisuals.Spark, false);
        _ambient.SetAmbience(uid, false);
        if (args.Cancelled)
            return;
        component.Code = "";
        UpdateUserInterface(uid, component);
        _audio.PlayPvs(component.AccessGrantedSound, uid);
    }

    /// <summary>
    /// When examine -> add info about maintenance panel
    /// </summary>
    private void OnExamine(EntityUid uid, DigitalLockComponent component, ExaminedEvent args)
    {
        var message = new FormattedMessage();
        if (component.MaintenanceOpen)
            message.AddMarkupOrThrow(Loc.GetString("digital-lock-examine-maintenance-open"));
        else
            message.AddMarkupOrThrow(Loc.GetString("digital-lock-examine-maintenance-closed"));
        args.PushMessage(message);
    }

    #endregion

    #region UI

    /// <summary>
    /// Update UI Status on init
    /// </summary>
    private void OnInit(EntityUid uid, DigitalLockComponent component, ComponentInit args)
    {
        UpdateStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// When enter button pressed -> update state
    /// </summary>
    private void OnEnterButtonPressed(EntityUid uid, DigitalLockComponent component, DigitalLockKeypadEnterMessage args)
    {
        UpdateStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// When keypad button pressed -> add number to entered code, update state
    /// </summary>
    private void OnKeypadButtonPressed(EntityUid uid, DigitalLockComponent component, DigitalLockKeypadMessage args)
    {
        component.LastPlayedKeypadSemitones = PlayKeypadSound(uid, args.Value, component.LastPlayedKeypadSemitones, component.KeypadPressSound);

        if (!IsAwaitingInput(component.Status)
            || component.EnteredCode.Length >= component.MaxCodeLength)
            return;

        component.EnteredCode += args.Value.ToString();
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Handle clear button.
    /// </summary>
    private void OnClearButtonPressed(EntityUid uid, DigitalLockComponent component, DigitalLockKeypadClearMessage args)
    {
        _audio.PlayPvs(component.KeypadPressSound, uid);

        switch (component.Status)
        {
            case DigitalLockStatus.CHANGE_MODE_CONFIRMATION:
                component.Status = DigitalLockStatus.AWAIT_CODE;
                UpdateUserInterface(uid, component);
                break;
            case DigitalLockStatus.CHANGE_MODE_CODE:
                component.Status = DigitalLockStatus.CHANGE_MODE_CANCEL_CONFIRMATION;
                UpdateUserInterface(uid, component);
                break;
            case DigitalLockStatus.CHANGE_MODE_CANCEL_CONFIRMATION:
                component.Status = DigitalLockStatus.CHANGE_MODE_CANCEL_CONFIRMATION;
                UpdateUserInterface(uid, component);
                break;
            case DigitalLockStatus.AWAIT_CODE:
            case DigitalLockStatus.AWAIT_CONFIRMATION:
                if (component.EnteredCode == "")
                    component.Status = DigitalLockStatus.CHANGE_MODE_CONFIRMATION;
                else
                    component.EnteredCode = "";
                UpdateUserInterface(uid, component);
                break;
        }
    }

    /// <summary>
    /// Update Status of Digital lock
    /// </summary>
    /// <param name="uid">Lock Owner</param>
    /// <param name="component">Digital Lock Component</param>
    private void UpdateStatus(EntityUid uid, DigitalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<LockComponent>(uid, out var lockComponent))
            return;

        switch (component.Status)
        {
            case DigitalLockStatus.AWAIT_CODE:
                if (component.Code == "" && component.EnteredCode != "")
                {
                    component.Code = component.EnteredCode;
                    component.EnteredCode = "";
                    component.Status = DigitalLockStatus.AWAIT_CONFIRMATION;
                    break;
                }

                if (component.EnteredCode == component.Code && component.Code != "")
                {
                    component.Status = DigitalLockStatus.OPENED;
                    _audio.PlayPvs(component.AccessGrantedSound, uid);
                    if (_lock.IsLocked(uid))
                        _lock.Unlock(uid, null, lockComponent);
                }
                else if (component.EnteredCode != "")
                {
                    component.EnteredCode = "";
                    _audio.PlayPvs(component.AccessDeniedSound, uid);
                }

                break;
            case DigitalLockStatus.AWAIT_CONFIRMATION:
                if (component.EnteredCode == component.Code)
                {
                    component.EnteredCode = "";
                    component.Status = DigitalLockStatus.AWAIT_CODE;
                    _audio.PlayPvs(component.AccessGrantedSound, uid);
                }
                break;
            case DigitalLockStatus.OPENED:
                component.EnteredCode = "";
                if (!_lock.IsLocked(uid))
                    _lock.Lock(uid, null, lockComponent);
                component.Status = DigitalLockStatus.AWAIT_CODE;
                break;

            case DigitalLockStatus.CHANGE_MODE_CONFIRMATION:
                component.Status = DigitalLockStatus.CHANGE_MODE_CODE;
                break;

            case DigitalLockStatus.CHANGE_MODE_CANCEL_CONFIRMATION:
                component.Status = DigitalLockStatus.AWAIT_CODE;
                break;

            case DigitalLockStatus.CHANGE_MODE_CODE:
                if (component.EnteredCode == component.Code && component.Code != "")
                {
                    component.Code = "";
                    component.EnteredCode = "";
                    component.Status = DigitalLockStatus.AWAIT_CODE;
                    _audio.PlayPvs(component.AccessGrantedSound, uid);

                }
                else if (component.EnteredCode != "")
                {
                    component.EnteredCode = "";
                    _audio.PlayPvs(component.AccessDeniedSound, uid);
                }
                break;
        }
    }

    /// <summary>
    /// Update Digital Lock UI
    /// </summary>
    /// <param name="uid">Lock Owner</param>
    /// <param name="component">Digital Lock Component</param>
    private void UpdateUserInterface(EntityUid uid, DigitalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_ui.HasUi(uid, DigitalLockUiKey.Key))
            return;

        var state = new DigitalLockUiState
        {
            Status = component.Status,
            EnteredCodeLength = component.EnteredCode.Length,
            MaxCodeLength = component.Code.Length == 0 ? component.MaxCodeLength : component.Code.Length,
        };

        _ui.SetUiState(uid, DigitalLockUiKey.Key, state);
    }

    /// <summary>
    /// Plays sound when keypad button pressed.
    /// </summary>
    /// <param name="uid">Entity which plays sound</param>
    /// <param name="number">Number of button</param>
    public int PlayKeypadSound(EntityUid uid, int number, int lastPlayedKeypadSemitones, SoundSpecifier keypadPressSound)
    {
        // This is a C mixolydian blues scale.
        // 1 2 3    C D Eb
        // 4 5 6    E F F#
        // 7 8 9    G A Bb
        var semitoneShift = number switch
        {
            1 => 0,
            2 => 2,
            3 => 3,
            4 => 4,
            5 => 5,
            6 => 6,
            7 => 7,
            8 => 9,
            9 => 10,
            0 => lastPlayedKeypadSemitones + 12,
            _ => 0,
        };

        var opts = keypadPressSound.Params;
        opts = AudioHelpers.ShiftSemitone(opts, semitoneShift).AddVolume(-5f);
        _audio.PlayPvs(keypadPressSound, uid, opts);

        // Don't double-dip on the octave shifting
        return number == 0 ? lastPlayedKeypadSemitones : semitoneShift;
    }

    /// <summary>
    /// Returns true if we waiting input
    /// </summary>
    private bool IsAwaitingInput(DigitalLockStatus status) =>
        status is DigitalLockStatus.AWAIT_CODE
           or DigitalLockStatus.AWAIT_CONFIRMATION
           or DigitalLockStatus.CHANGE_MODE_CODE;

    #endregion
}