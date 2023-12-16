using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Shared.TapeRecorder;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.TapeRecorder;

public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{

    [Dependency] private readonly ChatSystem _chatSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(GetAltVerbs);
        SubscribeLocalEvent<RecordingTapeRecorderComponent, ListenEvent>(OnListen);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //Get all recording tape recorders, increment the cassette position
        var recorderQuery = EntityQueryEnumerator<RecordingTapeRecorderComponent, TapeRecorderComponent>();
        while (recorderQuery.MoveNext(out var uid, out var component, out var tapeRecorderComponent))
        {
            var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
            if (cassette == null)
                continue;

            //Stop if we reach the end of the tape
            if (TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            {
                var currentTime = tapeCassetteComponent.CurrentPosition + frameTime;

                //Remove any flushed messages in the interval we just recorded
                tapeCassetteComponent.RecordedData.RemoveAll(x => x.Timestamp > tapeCassetteComponent.CurrentPosition && x.Timestamp <= currentTime && x.Flushed);

                //Mark all messages we have passed as flushed
                foreach (var recordedMessage in tapeCassetteComponent.RecordedData.Where(x => x.Timestamp <= tapeCassetteComponent.CurrentPosition && !x.Flushed))
                {
                    recordedMessage.Flushed = true;
                }

                tapeCassetteComponent.CurrentPosition = currentTime;
                if (tapeCassetteComponent.CurrentPosition >= tapeCassetteComponent.MaxCapacity)
                    StopRecording(uid, tapeRecorderComponent);
            }
        }

        //Get all playing tape recorders, increment cassette position and play any messages from the interval
        var playerQuery = EntityQueryEnumerator<PlayingTapeRecorderComponent, TapeRecorderComponent>();
        while (playerQuery.MoveNext(out var uid, out var component, out var tapeRecorderComponent))
        {
            var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
            if (cassette == null)
                continue;

            if (TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            {
                //Get the segment of the tape to be played
                //And any messages within that time period
                var currentTime = tapeCassetteComponent.CurrentPosition + frameTime;
                foreach (var messageToBeReplayed in tapeCassetteComponent.RecordedData.Where(x => x.Timestamp > tapeCassetteComponent.CurrentPosition && x.Timestamp <= currentTime))
                {
                    _chatSystem.TrySendInGameICMessage(uid, messageToBeReplayed.Message, InGameICChatType.Speak, false, nameOverride: messageToBeReplayed.Name);
                }

                tapeCassetteComponent.CurrentPosition = currentTime;

                //Stop when we reach the end of the tape
                if (tapeCassetteComponent.CurrentPosition >= tapeCassetteComponent.MaxCapacity)
                    StopPlayback(uid, tapeRecorderComponent);
            }
        }

        var rewindingQuery = EntityQueryEnumerator<RewindingTapeRecorderComponent, TapeRecorderComponent>();
        while (rewindingQuery.MoveNext(out var uid, out var component, out var tapeRecorderComponent))
        {
            var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
            if (cassette == null)
                continue;

            //Stop if we reach the beginning of the tape
            if (TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            {
                var currentTime = Math.Max(0, tapeCassetteComponent.CurrentPosition - (frameTime * 3));

                tapeCassetteComponent.CurrentPosition = currentTime;
                if (tapeCassetteComponent.CurrentPosition <= float.Epsilon)
                    StopRewinding(uid, tapeRecorderComponent);
            }
        }

    }

    private void GetAltVerbs(EntityUid uid, TapeRecorderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
        if (cassette == null)
            return;

        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return;

        switch (component.Mode)
        {
            case TapeRecorderMode.Stopped:

                //If we have tape left to record or play on
                if (tapeCassetteComponent.MaxCapacity - tapeCassetteComponent.CurrentPosition > float.Epsilon)
                {
                    args.Verbs.Add(new AlternativeVerb()
                    {
                        Text = Loc.GetString("verb-tape-recorder-record"),
                        Act = () =>
                        {
                            StartRecording(uid, component);
                        },
                        Priority = 1
                    });
                    args.Verbs.Add(new AlternativeVerb()
                    {
                        Text = Loc.GetString("verb-tape-recorder-playback"),
                        Act = () =>
                        {
                            StartPlayback(uid, component);
                        },
                        Priority = 2
                    });
                }

                //If there is tape to rewind
                if (tapeCassetteComponent.CurrentPosition > float.Epsilon)
                {
                    args.Verbs.Add(new AlternativeVerb()
                    {
                        Text = Loc.GetString("verb-tape-recorder-rewind"),
                        Act = () =>
                        {
                            StartRewinding(uid, component);
                        },
                        Priority = 3
                    });
                }

                break;

            case TapeRecorderMode.Recording:
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-tape-recorder-stop"),
                    Act = () =>
                    {
                        StopRecording(uid, component);
                    },
                    Priority = 1
                });
                break;
            case TapeRecorderMode.Playing:
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-tape-recorder-stop"),
                    Act = () =>
                    {
                        StopPlayback(uid, component);
                    },
                    Priority = 1
                });
                break;
            case TapeRecorderMode.Rewinding:
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-tape-recorder-stop"),
                    Act = () =>
                    {
                        StopRewinding(uid, component);
                    },
                    Priority = 1
                });
                break;
        }
    }

    private void OnListen(EntityUid uid, RecordingTapeRecorderComponent component, ListenEvent args)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);

        if (cassette == null)
            return;


        if (TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
        {
            var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
            RaiseLocalEvent(args.Source, nameEv);

            tapeCassetteComponent.RecordedData.Add(new TapeCassetteRecordedMessage(tapeCassetteComponent.CurrentPosition, nameEv.Name, args.Message));
        }
    }
}
