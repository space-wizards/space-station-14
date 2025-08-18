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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DigitalLockComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<DigitalLockComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockMaintenanceOpenDoAfterEvent>(OnMaintenanceOpen);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockResetDoAfterEvent>(OnReset);
        SubscribeLocalEvent<DigitalLockComponent, ExaminedEvent>(OnExamine);

        // UI
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockKeypadMessage>(OnKeypadButtonPressed);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockKeypadClearMessage>(OnClearButtonPressed);
        SubscribeLocalEvent<DigitalLockComponent, DigitalLockKeypadEnterMessage>(OnEnterButtonPressed);
    }

    private void OnInit(EntityUid uid, DigitalLockComponent component, ComponentInit args)
    {
        UpdateStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, DigitalLockComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_tool.HasQuality(args.Used, "Screwing"))
            args.Handled = _tool.UseTool(args.Used, args.User, uid, 2f, "Screwing", new DigitalLockMaintenanceOpenDoAfterEvent());
        else if (_tool.HasQuality(args.Used, "Pulsing") && component.MaintenanceOpen && component.Code != "")
        {
            var codeLength = component.Code.Length;
            _appearance.SetData(uid, DigitalLockVisuals.Spark, true);
            if (TryComp<AmbientSoundComponent>(uid, out var ambientSound))
            {
                ambientSound.Enabled = true;
                Dirty(uid, ambientSound);
            }
            args.Handled = _electrocution.TryDoElectrocution(args.User, uid, 5, TimeSpan.FromSeconds(2), true) || _tool.UseTool(args.Used, args.User, uid, 4f * codeLength, "Pulsing", new DigitalLockResetDoAfterEvent());
        }

        args.Handled = false;
    }

    private void OnMaintenanceOpen(EntityUid uid, DigitalLockComponent component, DigitalLockMaintenanceOpenDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        component.MaintenanceOpen = !component.MaintenanceOpen;
        args.Handled = true;
    }

    private void OnReset(EntityUid uid, DigitalLockComponent component, DigitalLockResetDoAfterEvent args)
    {
        _appearance.SetData(uid, DigitalLockVisuals.Spark, false);
        if (TryComp<AmbientSoundComponent>(uid, out var ambientSound))
        {
            ambientSound.Enabled = false;
            Dirty(uid, ambientSound);
        }
        if (args.Cancelled)
            return;
        component.Code = "";
        UpdateUserInterface(uid, component);
        _audio.PlayPvs(component.AccessGrantedSound, uid);
    }

    private void OnExamine(EntityUid uid, DigitalLockComponent component, ExaminedEvent args)
    {
        var message = new FormattedMessage();
        if (component.MaintenanceOpen)
            message.AddMarkup(Loc.GetString("digital-lock-examine-maintenance-open"));
        else
            message.AddMarkup(Loc.GetString("digital-lock-examine-maintenance-closed"));
        args.PushMessage(message);
    }

    private void OnEnterButtonPressed(EntityUid uid, DigitalLockComponent component, DigitalLockKeypadEnterMessage args)
    {
        UpdateStatus(uid, component);
        UpdateUserInterface(uid, component);
    }

    private void OnKeypadButtonPressed(EntityUid uid, DigitalLockComponent component, DigitalLockKeypadMessage args)
    {
        PlayNukeKeypadSound(uid, args.Value, component);

        if (component.Status is not DigitalLockStatus.AWAIT_CODE
            and not DigitalLockStatus.AWAIT_CONFIRMATION
            and not DigitalLockStatus.CHANGE_MODE_CODE)
            return;

        if (component.EnteredCode.Length >= component.MaxCodeLength)
            return;

        component.EnteredCode += args.Value.ToString();
        UpdateUserInterface(uid, component);
    }

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

    private void PlayNukeKeypadSound(EntityUid uid, int number, DigitalLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

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
            0 => component.LastPlayedKeypadSemitones + 12,
            _ => 0,
        };

        // Don't double-dip on the octave shifting
        component.LastPlayedKeypadSemitones = number == 0 ? component.LastPlayedKeypadSemitones : semitoneShift;

        var opts = component.KeypadPressSound.Params;
        opts = AudioHelpers.ShiftSemitone(opts, semitoneShift).AddVolume(-5f);
        _audio.PlayPvs(component.KeypadPressSound, uid, opts);
    }
}