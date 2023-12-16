using Content.Server.CassetteTape.Components;
using Content.Server.Chat.Systems;
using Content.Server.PowerCell;
using Content.Server.Speech;
using Content.Server.SurveillanceCamera;
using Content.Shared.Actions;
using Content.Shared.CassetteTape;
using Content.Shared.CassetteTape.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;


namespace Content.Server.CassetteTape.EntitySystems;

[UsedImplicitly]
public sealed class CassettePlayheadSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private Dictionary<EntityUid, LinkedList<CassetteTapeAudioInfo>> CassetteAudioCache = new();
    private Dictionary<EntityUid, LinkedListNode<CassetteTapeAudioInfo>?> NextAudioEventCache = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<CassettePlayheadComponent, CassetteTapeChangedEvent>(OnInserted);
        SubscribeLocalEvent<CassettePlayheadComponent, ListenEvent>(OnHeardSpeech); // TODO: Make a generic Microphone component and system. Surveillance Cameras and Radios could both use it.
        SubscribeLocalEvent<CassettePlayheadComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<CassettePlayheadComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<CassettePlayheadComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnGetActions(EntityUid uid, CassettePlayheadComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private void OnToggleAction(EntityUid uid, CassettePlayheadComponent component, ToggleActionEvent args)
    {
        // Toggles play/record mode when idle.
        if (component.PlayheadState == CassettePlayheadState.Standby)
        {
            if (component.TargetActiveState == CassettePlayheadState.Recording)
            {
                _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-switch-mode-play"), uid);
                _audio.PlayPvs(component.StopClunkSound, uid);
                component.TargetActiveState = CassettePlayheadState.Playing;
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-switch-mode-record"), uid);
                _audio.PlayPvs(component.StopClunkSound, uid);
                component.TargetActiveState = CassettePlayheadState.Recording;
            }
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-stopped-early-message"), uid);
            _audio.PlayPvs(component.StopClunkSound, uid);
            SetPlayheadOperationState(uid, CassettePlayheadState.Standby, component);
        }
    }


    private void OnActivateInWorld(EntityUid uid, CassettePlayheadComponent component, ActivateInWorldEvent args)
    {
        if (component.PlayheadState == CassettePlayheadState.Standby)
        {
            if (component.TargetActiveState == CassettePlayheadState.Recording)
            {
                TryStartRecording(uid, component);
            }
            else if (component.TargetActiveState == CassettePlayheadState.Playing)
            {
                TryStartPlaying(uid, component);
            }
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-stopped-early-message"), uid);
            _audio.PlayPvs(component.StopClunkSound, uid);
            SetPlayheadOperationState(uid, CassettePlayheadState.Standby, component);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CassettePlayheadComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            switch (comp.PlayheadState)
            {
                case CassettePlayheadState.Standby:
                    continue;
                case CassettePlayheadState.Playing:
                {
                    // Read event stream from cache and start playing back speech audio.
                    if (CassetteAudioCache.ContainsKey(ent) && NextAudioEventCache.ContainsKey(ent))
                    {
                        if (NextAudioEventCache[ent] != null)
                        {
                            var audioEvent = NextAudioEventCache[ent]?.Value;
                            if (audioEvent != null && comp.PlayheadLocation >= audioEvent.Value.StartTime)
                            {
                                Speak(ent, comp, audioEvent.Value.Speaker, audioEvent.Value.SpokenMessage);
                                NextAudioEventCache[ent] = NextAudioEventCache[ent]?.Next;
                            }
                        }
                    }

                    AdvancePlayheadLocation(ent, comp, frameTime);
                    continue;
                }

                case CassettePlayheadState.Recording:
                {
                    AdvancePlayheadLocation(ent, comp, frameTime);
                    continue;
                }
            }
        }
    }

    public void Speak(EntityUid uid, CassettePlayheadComponent component, string Speaker, string Message)
    {
        // TODO: Speech sounds.
        Log.Debug($"Tried to speak from cassette {uid}, {Speaker}, {Message}");
        var name = Loc.GetString("speech-name-relay", ("speaker", Name(uid)),
            ("originalName", Speaker));

        // Log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many tape players.
        _chatSystem.TrySendInGameICMessage(uid, Message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, nameOverride: name);
    }

    public void AdvancePlayheadLocation(EntityUid uid, CassettePlayheadComponent component, float frameTime)
    {
        if (!component.HasTape)
            return;

        component.PlayheadLocation += frameTime;
        if (component.PlayheadLocation > component.CurrentTape?.LengthSeconds && component.PlayheadState != CassettePlayheadState.Standby)
        {
            _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-reached-end-message"), uid);
            _audio.PlayPvs(component.StopClunkSound, uid);
            SetPlayheadOperationState(uid, CassettePlayheadState.Standby, component);
        }
    }

    public void OnInserted(EntityUid uid, CassettePlayheadComponent component, CassetteTapeChangedEvent args)
    {
        component.HasTape = !args.Ejected;
        component.CurrentTape = null;
        component.PlayheadLocation = 0.0f;

        if (component.HasTape)
        {
            CassetteTapeComponent? tape = null;
            if (Resolve(args.Tape, ref tape))
            {
                component.CurrentTape = tape;
            }
        }

        SetPlayheadOperationState(uid, CassettePlayheadState.Standby, component);
    }

    public void OnHeardSpeech(EntityUid uid, CassettePlayheadComponent component, ListenEvent args)
    {
        // Do we have a tape inserted? Are we recording?
        if (!component.HasTape || component.PlayheadState != CassettePlayheadState.Recording)
            return;

        StoreSpeechOnTape(uid, args.Source, args.Message, component.PlayheadLocation, component.CurrentTape);
    }

    public bool TryStartPlaying(EntityUid uid, CassettePlayheadComponent? playheadComponent = null)
    {
        if (!Resolve(uid, ref playheadComponent))
            return false;

        // Already playing / recording?
        if (playheadComponent.PlayheadState != CassettePlayheadState.Standby)
            return false;

        // Do we have a tape inserted?
        if (!playheadComponent.HasTape)
        {
            _audio.PlayPvs(playheadComponent.StopClunkSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-no-tape-message"), uid);
            return false;
        }

        // Transform audio events from the tape into a flattened stream and cache it.
        LinkedList<CassetteTapeAudioInfo> flattenedAudioEvents = new();
        if (playheadComponent.CurrentTape != null && playheadComponent.CurrentTape.StoredAudioData.Count > 0)
        {
            Log.Debug($"Flattening Audio events!");
            var sortedAudioData = playheadComponent.CurrentTape.StoredAudioData;
            sortedAudioData.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            var currentNode = flattenedAudioEvents.AddFirst(sortedAudioData[0]);

            for (int i = 1; i < sortedAudioData.Count; i++)
            {
                currentNode = flattenedAudioEvents.AddAfter(currentNode, sortedAudioData[i]);
            }
        }

        CassetteAudioCache[uid] = flattenedAudioEvents;
        NextAudioEventCache[uid] = flattenedAudioEvents.First;

        // Start playing.
        _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-starts-moving-message"), uid);
        _audio.PlayPvs(playheadComponent.PlayMotorSound, uid);
        playheadComponent.PlayheadLocation = 0.0f;
        SetPlayheadOperationState(uid, CassettePlayheadState.Playing, playheadComponent);


        return true;
    }

    public bool TryStartRecording(EntityUid uid, CassettePlayheadComponent? playheadComponent = null)
    {
        if (!Resolve(uid, ref playheadComponent))
            return false;

        // Already playing / recording?
        if (playheadComponent.PlayheadState != CassettePlayheadState.Standby)
            return false;

        // Do we have a tape inserted?
        if (!playheadComponent.HasTape)
        {
            _audio.PlayPvs(playheadComponent.StopClunkSound, uid);
            _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-no-tape-message"), uid);
            return false;
        }

        // Start recording.
        _popupSystem.PopupEntity(Loc.GetString("cassette-playhead-starts-moving-message"), uid);
        _audio.PlayPvs(playheadComponent.PlayMotorSound, uid);
        playheadComponent.PlayheadLocation = 0.0f;
        SetPlayheadOperationState(uid, CassettePlayheadState.Recording, playheadComponent);

        return true;
    }

    public void SetPlayheadOperationState(EntityUid uid, CassettePlayheadState newState, CassettePlayheadComponent? playheadComponent = null)
    {
        if (!Resolve(uid, ref playheadComponent))
            return;

        playheadComponent.PlayheadState = newState;
        if (newState == CassettePlayheadState.Standby)
        {
            playheadComponent.PlayheadLocation = 0.0f;
        }
    }


    public void StoreSpeechOnTape(EntityUid uid, EntityUid speaker, string speech, float timeStart, CassetteTapeComponent? tape = null, CassettePlayheadComponent? playheadComponent = null)
    {
        if (!Resolve(uid, ref playheadComponent))
            return;

        if(tape == null)
            return;

        // Can't store speech if there's no tape to store it in!
        if (timeStart > tape._lengthSeconds)
            return;


        int numChars = speech.Length;
        float timeNeededToSpeak = numChars / (float)CassetteTapeSystem.CharactersSpokenPerSecond;
        float speechLength = timeNeededToSpeak;

        // Cut off end of speech if tape isn't long enough.
        if (timeStart + timeNeededToSpeak > tape._lengthSeconds)
        {
            speechLength = speechLength - (tape._lengthSeconds - timeStart);
        }

        // Get max number of words for speech length.
        int maxCharsSpoken = numChars;
        if (speechLength != timeNeededToSpeak)
            maxCharsSpoken = (int)Math.Floor(speechLength * CassetteTapeSystem.CharactersSpokenPerSecond);

        // Truncate the string to the maximum spoken, plus a couple of dashes for flavour.
        // e.g, "Help help, we need backup in Security!" -> "Help help, we need backup in Se-- "
        string storedSpeech = speech.Substring(maxCharsSpoken) + "-- ";

        // Get the speakers (current!) name.
        var nameEv = new TransformSpeakerNameEvent(speaker, Name(speaker));
        RaiseLocalEvent(speaker, nameEv);

        // Store the data in the tape. Note; this will generate overlapping events if the player
        // rewinds the tape and records in another spot on top of an existing recording.
        tape.StoredAudioData.Add(new CassetteTapeAudioInfo
        {
            SpokenMessage = storedSpeech,
            Speaker = nameEv.Name,
            EntryLength = speechLength,
            StartTime = timeStart
        });
    }
}
