using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.VoiceRecorder;
using Robust.Server.Audio;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Server.VoiceRecorder;

public sealed class VoiceRecorderSystem : SharedVoiceRecordedSystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelaySystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoiceRecorderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VoiceRecorderComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<VoiceRecorderComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<VoiceRecorderComponent, GetVerbsEvent<ActivationVerb>>(OnClearVerb); 
        SubscribeLocalEvent<VoiceRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnPrintVerb);
        SubscribeLocalEvent<VoiceRecorderComponent, VoiceRecorderCleaningDoAfterEvent>(OnClearRecordDoAfter);
    }

    private void OnInit(EntityUid uid, VoiceRecorderComponent component, ComponentInit args)
    {
        if (component.IsRecording)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.Range;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);

        UpdateAppearance(uid, component);
    }

    private void OnListen(EntityUid uid, VoiceRecorderComponent component, ref ListenEvent args)
    {
        var message = args.Message.Trim();

        if (!component.IsRecording)
            return;

        var listenEv = new ListenAttemptEvent(args.Source);
        RaiseLocalEvent(uid, listenEv);

        if (listenEv.Cancelled)
            return;

        if (string.IsNullOrWhiteSpace(message))
            return;

        var transformEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, transformEv);

        component.RecordedText.Add(Loc.GetString("voice-recorder-message-text",
            ("time", component.RecordTime.ToString(@"hh\:mm\:ss")),
            ("source", transformEv.Name),
            ("message", message)));
    }

    private void OnActivate(EntityUid uid, VoiceRecorderComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        EnsureComp<UseDelayComponent>(uid, out var delay);

        if (component.IsRecording)
        {
            StopRecord(uid, component, args.User);
            _useDelaySystem.TryResetDelay((uid, delay));
        }
        else
        {
            StartRecord(uid, component, args.User);
            _useDelaySystem.TryResetDelay((uid, delay));
        }
    }

    private void OnClearVerb(EntityUid uid, VoiceRecorderComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (component.CancelToken != null || !args.CanAccess || !args.CanInteract)
            return;

        if (component.IsRecording)
            return;

        if (component.RecordedText.Count <= 0)
            return;

        var verb = new ActivationVerb()
        {
            Act = () => ClearRecord(uid, component, args.User),
            IconEntity = GetNetEntity(uid),
            Text = Loc.GetString("voice-recorder-cleaning-verb-text"),
            Message = Loc.GetString("voice-recorder-cleaning-verb-message")
        };

        args.Verbs.Add(verb);
    }

    private void OnPrintVerb(EntityUid uid, VoiceRecorderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (component.IsRecording)
            return;

        if (component.RecordedText.Count <= 0)
            return;

        if (_gameTiming.CurTime < component.PrintCooldownEnd)
            return;

        var verb = new AlternativeVerb()
        {
            Act = () => PrintRecord(uid, component, args.User),
            IconEntity = GetNetEntity(uid),
            Text = Loc.GetString("voice-recorder-print-verb-text"),
            Message = Loc.GetString("voice-recorder-print-verb-message")
        };

        args.Verbs.Add(verb);
    }
    private void StartRecord(EntityUid uid, VoiceRecorderComponent component, EntityUid user)
    {
        component.IsRecording = true;
        EnsureComp<ActiveListenerComponent>(uid).Range = component.Range;

        component.RecordedText.Add(Loc.GetString("voice-recorder-message-record-start-text", ("time", component.RecordTime.ToString(@"hh\:mm\:ss"))));
        _audioSystem.PlayPvs(component.RecordingStartSound, uid);
        UpdateAppearance(uid, component);
    }

    private void StopRecord(EntityUid uid, VoiceRecorderComponent component, EntityUid user)
    {
        component.IsRecording = false;
        RemCompDeferred<ActiveListenerComponent>(uid);

        component.RecordedText.Add(Loc.GetString("voice-recorder-message-record-stop-text", ("time", component.RecordTime.ToString(@"hh\:mm\:ss"))));
        _audioSystem.PlayPvs(component.RecordingStopSound, uid);
        UpdateAppearance(uid, component);
    }

    private void ClearRecord(EntityUid uid, VoiceRecorderComponent component, EntityUid user)
    {
        _popupSystem.PopupEntity(Loc.GetString("voice-recorder-cleaning-warning"), uid, user, Shared.Popups.PopupType.SmallCaution);

        var doAfterEv = new VoiceRecorderCleaningDoAfterEvent();
        var doAfterArgs = new DoAfterArgs(EntityManager, user, 5.0f, doAfterEv, uid)
        {
            BreakOnHandChange = true,
            NeedHand = true
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnClearRecordDoAfter(EntityUid uid, VoiceRecorderComponent component, VoiceRecorderCleaningDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (component.IsRecording)
            StopRecord(uid, component, args.User);

        _audioSystem.PlayPvs(component.RecordingEraseSound, uid);
        component.RecordedText.Clear();
        component.RecordTime = TimeSpan.Zero;
    }

    private void PrintRecord(EntityUid uid, VoiceRecorderComponent component, EntityUid user)
    {
        if (component.IsRecording)
            StopRecord(uid, component, user);

        var text = new StringBuilder();
        var paper = EntityManager.SpawnEntity(component.PaperPrototype, Transform(uid).Coordinates);
        _handsSystem.PickupOrDrop(user, paper, checkActionBlocker: false);

        if (!HasComp<PaperComponent>(paper))
            return;

        _metaDataSystem.SetEntityName(paper, Loc.GetString("voice-recorder-paper-title"));

        foreach (var message in component.RecordedText)
        {
            text.AppendLine(message);
        }

        _paperSystem.SetContent(paper, text.ToString());

        component.PrintCooldownEnd = _gameTiming.CurTime + component.PrintCooldown;
        _audioSystem.PlayPvs(component.PrintSound, uid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var recorderQuery = EntityQueryEnumerator<VoiceRecorderComponent>();
        while (recorderQuery.MoveNext(out var recorder))
        {
            if (recorder.IsRecording)
                recorder.RecordTime += TimeSpan.FromSeconds(frameTime);
        }
    }
}
