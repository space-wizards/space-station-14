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
        SubscribeLocalEvent<TriggerOnVoiceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TriggerOnVoiceComponent, ExaminedEvent>(OnVoiceExamine);
        SubscribeLocalEvent<TriggerOnVoiceComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TriggerOnVoiceComponent, GetVerbsEvent<AlternativeVerb>>(OnVoiceGetAltVerbs);
    }

    private void OnMapInit(Entity<TriggerOnVoiceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.DefaultKeyPhrase != null)
        {
            ent.Comp.KeyPhrase = Loc.GetString(ent.Comp.DefaultKeyPhrase);
            Dirty(ent);
        }

        UpdateListening(ent);
    }

    private void OnVoiceExamine(EntityUid uid, TriggerOnVoiceComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !component.ShowExamine)
            return;

        if (component.InspectUninitializedLoc != null && string.IsNullOrWhiteSpace(component.KeyPhrase))
        {
            args.PushText(Loc.GetString(component.InspectUninitializedLoc));
        }
        else if (component.InspectInitializedLoc != null && !string.IsNullOrWhiteSpace(component.KeyPhrase))
        {
            args.PushText(Loc.GetString(component.InspectInitializedLoc.Value, ("keyphrase", component.KeyPhrase)));
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
        if (!args.CanInteract || !args.CanAccess || !ent.Comp.ShowVerbs)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(ent.Comp.IsRecording ? ent.Comp.StopRecordingVerb : ent.Comp.StartRecordingVerb),
            Act = () =>
            {
                if (ent.Comp.IsRecording)
                    StopRecording(ent, user);
                else
                    StartRecording(ent, user);
            },
            Priority = 1
        });

        if (ent.Comp.DefaultKeyPhrase != null
            && ent.Comp.KeyPhrase != Loc.GetString(ent.Comp.DefaultKeyPhrase))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString(ent.Comp.ResetRecordingVerb),
                Act = () =>
                {
                    SetToDefault(ent, user);
                },
            });
        }

        if (string.IsNullOrWhiteSpace(ent.Comp.KeyPhrase))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(ent.Comp.ClearRecordingVerb),
            Act = () =>
            {
                ClearRecording(ent);
            }
        });
    }

    /// <summary>
    /// Updates the presence/absence of the ActiveListenerComponent based on IsListening.
    /// </summary>
    private void UpdateListening(Entity<TriggerOnVoiceComponent> ent)
    {
        if (ent.Comp.IsListening)
            EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(ent);
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

    /// <summary>
    /// Resets the current key phrase to default.
    /// </summary>
    public void SetToDefault(Entity<TriggerOnVoiceComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.DefaultKeyPhrase == null)
            return;

        ent.Comp.KeyPhrase = Loc.GetString(ent.Comp.DefaultKeyPhrase);
        ent.Comp.IsRecording = false;
        Dirty(ent);
        UpdateListening(ent);

        _adminLogger.Add(LogType.Trigger, LogImpact.Low,
            $"A voice-trigger on {ToPrettyString(ent):entity} has been reset to default keyphrase: '{ent.Comp.KeyPhrase}'. User: {ToPrettyString(user):speaker}");

        _popup.PopupPredicted(Loc.GetString("trigger-on-voice-set-default", ("keyphrase", ent.Comp.KeyPhrase)), ent, user);
    }
}
