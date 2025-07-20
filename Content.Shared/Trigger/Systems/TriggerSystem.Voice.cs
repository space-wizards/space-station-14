using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Verbs;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem
{
    private void InitializeVoice()
    {
        SubscribeLocalEvent<TriggerOnVoiceComponent, ComponentInit>(OnVoiceInit);
        SubscribeLocalEvent<TriggerOnVoiceComponent, ExaminedEvent>(OnVoiceExamine);
        SubscribeLocalEvent<TriggerOnVoiceComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TriggerOnVoiceComponent, GetVerbsEvent<AlternativeVerb>>(OnVoiceGetAltVerbs);
    }

    private void OnVoiceInit(Entity<TriggerOnVoiceComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.IsListening)
            EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(ent);
    }

    private void OnVoiceExamine(Entity<TriggerOnVoiceComponent> ent, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            args.PushText(string.IsNullOrWhiteSpace(ent.Comp.KeyPhrase)
                ? Loc.GetString("trigger-on-voice-uninitialized")
                : Loc.GetString("trigger-on-voice-examine", ("keyphrase", ent.Comp.KeyPhrase)));
        }
    }
    private void OnListen(Entity<TriggerOnVoiceComponent> ent, ref ListenEvent args)
    {
        var component = ent.Comp;
        var message = args.Message.Trim();

        if (component.IsRecording)
        {
            var ev = new ListenAttemptEvent(args.Source);
            RaiseLocalEvent(ent, ev);

            if (ev.Cancelled)
                return;

            if (message.Length >= component.MinLength && message.Length <= component.MaxLength)
                FinishRecording(ent, args.Source, args.Message);
            else if (message.Length > component.MaxLength)
                _popup.PopupEntity(Loc.GetString("trigger-on-voice-record-failed-too-long"), ent);
            else if (message.Length < component.MinLength)
                _popup.PopupEntity(Loc.GetString("trigger-on-voice-record-failed-too-short"), ent);

            return;
        }

        if (!string.IsNullOrWhiteSpace(component.KeyPhrase) && message.IndexOf(component.KeyPhrase, StringComparison.InvariantCultureIgnoreCase) is var index and >= 0)
        {
            _adminLogger.Add(LogType.Trigger, LogImpact.Medium,
                    $"A voice-trigger on {ToPrettyString(ent):entity} was triggered by {ToPrettyString(args.Source):speaker} speaking the key-phrase {component.KeyPhrase}.");
            Trigger(ent, args.Source, ent.Comp.KeyOut);

            var messageWithoutPhrase = message.Remove(index, component.KeyPhrase.Length).Trim();
            var voice = new VoiceTriggeredEvent(args.Source, message, messageWithoutPhrase);
            RaiseLocalEvent(ent, ref voice);
        }
    }

    private void OnVoiceGetAltVerbs(Entity<TriggerOnVoiceComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(ent.Comp.IsRecording ? "trigger-on-voice-stop" : "trigger-on-voice-record"),
            Act = () =>
            {
                if (ent.Comp.IsRecording)
                    StopRecording(ent, user);
                else
                    StartRecording(ent, user);
            },
            Priority = 1
        });

        if (string.IsNullOrWhiteSpace(ent.Comp.KeyPhrase))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("trigger-on-voice-clear"),
            Act = () =>
            {
                ClearRecording(ent);
            }
        });
    }

    /// <summary>
    /// Start recording a new keyphrase.
    /// </summary>
    public void StartRecording(Entity<TriggerOnVoiceComponent> ent, EntityUid? user)
    {
        ent.Comp.IsRecording = true;
        Dirty(ent);
        EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;

        if (user == null)
            _adminLogger.Add(LogType.Trigger, LogImpact.Low, $"A voice-trigger on {ToPrettyString(ent):entity} has started recording.");
        else
            _adminLogger.Add(LogType.Trigger, LogImpact.Low, $"A voice-trigger on {ToPrettyString(ent):entity} has started recording. User: {ToPrettyString(user.Value):user}");

        _popup.PopupPredicted(Loc.GetString("trigger-on-voice-start-recording"), ent, user);
    }

    /// <summary>
    /// Stop recording without setting a keyphrase.
    /// </summary>
    public void StopRecording(Entity<TriggerOnVoiceComponent> ent, EntityUid? user)
    {
        ent.Comp.IsRecording = false;
        Dirty(ent);
        if (string.IsNullOrWhiteSpace(ent.Comp.KeyPhrase))
            RemComp<ActiveListenerComponent>(ent);

        _popup.PopupPredicted(Loc.GetString("trigger-on-voice-stop-recording"), ent, user);
    }


    /// <summary>
    /// Stop recording and set the current keyphrase message.
    /// </summary>
    public void FinishRecording(Entity<TriggerOnVoiceComponent> ent, EntityUid source, string message)
    {
        ent.Comp.KeyPhrase = message;
        ent.Comp.IsRecording = false;
        Dirty(ent);

        _adminLogger.Add(LogType.Trigger, LogImpact.Low,
                $"A voice-trigger on {ToPrettyString(ent):entity} has recorded a new keyphrase: '{ent.Comp.KeyPhrase}'. Recorded from {ToPrettyString(source):speaker}");

        _popup.PopupEntity(Loc.GetString("trigger-on-voice-recorded", ("keyphrase", ent.Comp.KeyPhrase)), ent);
    }

    /// <summary>
    /// Resets the key phrase and stops recording.
    /// </summary>
    public void ClearRecording(Entity<TriggerOnVoiceComponent> ent)
    {
        ent.Comp.KeyPhrase = null;
        ent.Comp.IsRecording = false;
        Dirty(ent);
        RemComp<ActiveListenerComponent>(ent);
    }
}
