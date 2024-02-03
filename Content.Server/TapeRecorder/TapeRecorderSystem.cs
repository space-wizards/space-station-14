using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Server.VoiceMask;
using Content.Shared.TapeRecorder;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.Verbs;
using System.Linq;

namespace Content.Server.TapeRecorder;

public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecordingTapeRecorderComponent, ListenEvent>(OnListen);
    }

    protected override bool ProcessRecordingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        if (!base.ProcessRecordingTapeRecorder(tapeRecorder, frameTime))
        {
            StopRecording(tapeRecorder);
            SetMode(tapeRecorder, TapeRecorderMode.Stopped, false);
            return false;
        }

        return true;
    }

    protected override bool ProcessPlayingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        if (!base.ProcessPlayingTapeRecorder(tapeRecorder, frameTime))
        {
            StopPlayback(tapeRecorder);
            SetMode(tapeRecorder, TapeRecorderMode.Stopped, false);
            return false;
        }

        return true;
    }

    protected override bool ProcessRewindingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        if (!base.ProcessRewindingTapeRecorder(tapeRecorder, frameTime))
        {
            StopRewinding(tapeRecorder);
            SetMode(tapeRecorder, TapeRecorderMode.Stopped, false);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Given a time range, play all messages on a tape within said range
    /// Split into this system as shared does not have _chatSystem access
    /// </summary>
    protected override void ReplayMessagesInSegment(Entity<TapeRecorderComponent> tapeRecorder, TapeCassetteComponent tapeCassetteComponent, float segmentStart, float segmentEnd)
    {
        TryComp<VoiceMaskComponent>(tapeRecorder, out var voiceMaskComponent);

        foreach (var messageToBeReplayed in tapeCassetteComponent.RecordedData.Where(x => x.Timestamp > tapeCassetteComponent.CurrentPosition && x.Timestamp <= segmentEnd))
        {
            //Change the voice to match the speaker
            if (voiceMaskComponent != null)
                voiceMaskComponent.VoiceName = messageToBeReplayed.Name;
            //Play the message
            _chatSystem.TrySendInGameICMessage(tapeRecorder, messageToBeReplayed.Message, InGameICChatType.Speak, false);
        }
    }

   

    /// <summary>
    /// Whenever someone speaks within listening range, record it to tape
    /// </summary>
    private void OnListen(Entity<RecordingTapeRecorderComponent> tapeRecorder, ref ListenEvent args)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(tapeRecorder, SlotName);

        if (cassette == null)
            return;


        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return;

        //Handle someone using a voice changer
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);

        //Add a new entry to the tape
        tapeCassetteComponent.RecordedData.Add(new TapeCassetteRecordedMessage(tapeCassetteComponent.CurrentPosition, nameEv.Name, args.Message));
    }


    /// <summary>
    /// Start playback if we are not already playing, ensure we have a voice mask component so the name is correctly shown through radios
    /// </summary>
    protected override bool StartPlayback(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (base.StartPlayback(tapeRecorder, user))
        {
            EnsureComp<VoiceMaskComponent>(tapeRecorder);
            return true;
        }

        return false;
    }
}
