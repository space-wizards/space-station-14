using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Paper;
using Content.Server.Speech;
using Content.Server.VoiceMask;
using Content.Shared.TapeRecorder;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.TapeRecorder.Events;
using Robust.Server.Audio;
using System.Linq;
using System.Text;

namespace Content.Server.TapeRecorder;

public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{

    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TapeRecorderComponent, PrintTapeRecorderMessage>(OnPrintMessage);
    }

    protected override bool ProcessRecordingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        if (!base.ProcessRecordingTapeRecorder(tapeRecorder, frameTime))
        {
            Stop(tapeRecorder, changeMode: true);
            return false;
        }

        return true;
    }

    protected override bool ProcessPlayingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        if (!base.ProcessPlayingTapeRecorder(tapeRecorder, frameTime))
        {
            Stop(tapeRecorder, changeMode: true);
            return false;
        }

        return true;
    }

    protected override bool ProcessRewindingTapeRecorder(Entity<TapeRecorderComponent> tapeRecorder, float frameTime)
    {
        if (!base.ProcessRewindingTapeRecorder(tapeRecorder, frameTime))
        {
            Stop(tapeRecorder, changeMode: true);
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
        EnsureComp<VoiceMaskComponent>(tapeRecorder, out var voiceMaskComponent);

        foreach (var messageToBeReplayed in tapeCassetteComponent.RecordedData.Where(x => x.Timestamp > tapeCassetteComponent.CurrentPosition && x.Timestamp <= segmentEnd))
        {
            //Change the voice to match the speaker
            if (voiceMaskComponent != null)
                voiceMaskComponent.VoiceName = messageToBeReplayed.Name ?? messageToBeReplayed.DefaultName;
            //Play the message
            _chatSystem.TrySendInGameICMessage(tapeRecorder, messageToBeReplayed.Message, InGameICChatType.Speak, false);
        }
    }

    /// <summary>
    /// Whenever someone speaks within listening range, record it to tape
    /// </summary>
    private void OnListen(Entity<TapeRecorderComponent> tapeRecorder, ref ListenEvent args)
    {
        if (tapeRecorder.Comp.Mode != TapeRecorderMode.Recording || !tapeRecorder.Comp.Active)
            return;

        if (!TryGetTapeCassette(tapeRecorder, out var cassette))
            return;

        //Handle someone using a voice changer
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);

        //Add a new entry to the tape
        cassette.Comp.Buffer.Add(new TapeCassetteRecordedMessage(cassette.Comp.CurrentPosition, nameEv.Name, args.Message));
    }


    /// <summary>
    /// Start playback if we are not already playing, ensure we have a voice mask component so the name is correctly shown through radios
    /// </summary>
    protected override bool StartPlayback(Entity<TapeRecorderComponent> tapeRecorder, EntityUid? user = null)
    {
        if (base.StartPlayback(tapeRecorder, user))
        {
            //Server only component
            EnsureComp<VoiceMaskComponent>(tapeRecorder);
            return true;
        }

        return false;
    }

    private void OnPrintMessage(EntityUid uid, TapeRecorderComponent tapeRecorder, PrintTapeRecorderMessage args)
    {
        var text = new StringBuilder();
        var paper = EntityManager.SpawnEntity(tapeRecorder.PaperPrototype, Transform(uid).Coordinates);

        if (!TryGetTapeCassette(uid, out var cassette))
            return;

        // Sorting list by time for overwrite order
        var data = cassette.Comp.RecordedData;
        data.Sort((x,y) => x.Timestamp.CompareTo(y.Timestamp));

        // Looking if player's entity exists to give paper in its hand
        var player = args.Actor;
        if (Exists(player))
            _handsSystem.PickupOrDrop(player, paper, checkActionBlocker: false);

        if (!HasComp<PaperComponent>(paper))
            return;

        _metaDataSystem.SetEntityName(paper, Loc.GetString("tape-recorder-transcript-title"));

        _audioSystem.PlayPvs(tapeRecorder.PrintSound, uid);

        text.AppendLine(Loc.GetString("tape-recorder-print-start-text"));
        text.AppendLine();
        foreach (var message in cassette.Comp.RecordedData)
        {
            var name = "Unknown";
            var time = TimeSpan.FromSeconds((double) message.Timestamp);

            if (message.Name != null)
                name = message.Name;

            text.AppendLine(Loc.GetString("tape-recorder-print-message-text",
                ("time", time.ToString(@"hh\:mm\:ss")),
                ("source", name),
                ("message", message.Message)));
        }
        text.AppendLine();
        text.Append(Loc.GetString("tape-recorder-print-end-text"));

        _paperSystem.SetContent(paper, text.ToString());
    }
}
