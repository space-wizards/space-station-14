using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Server.VoiceMask;
using Content.Shared.TapeRecorder;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using System.Linq;

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

    /// <summary>
    /// Given a time range, play all messages on a tape within said range
    /// Split into this system as shared does not have _chatSystem access
    /// </summary>
    protected override void ReplayMessagesInSegment(EntityUid uid, TapeRecorderComponent tapeRecorderComponent, TapeCassetteComponent tapeCassetteComponent, float segmentStart, float segmentEnd)
    {
        TryComp<VoiceMaskComponent>(uid, out var voiceMaskComponent);

        foreach (var messageToBeReplayed in tapeCassetteComponent.RecordedData.Where(x => x.Timestamp > tapeCassetteComponent.CurrentPosition && x.Timestamp <= segmentEnd))
        {
            //Change the voice to match the speaker
            if (voiceMaskComponent != null)
                voiceMaskComponent.VoiceName = messageToBeReplayed.Name;
            //Play the message
            _chatSystem.TrySendInGameICMessage(uid, messageToBeReplayed.Message, InGameICChatType.Speak, false);
        }
    }

    /// <summary>
    /// Right click menu to swap mode
    /// </summary>
    private void GetAltVerbs(EntityUid uid, TapeRecorderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        //If no tape is loaded, show no options
        var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);
        if (cassette == null)
            return;

        //Sanity check, this is a tape? right?
        if (!TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
            return;

        //Dont allow mode changes when the mode is active
        if (component.Active)
            return;

        //If we have tape capacity remaining
        if (tapeCassetteComponent.MaxCapacity > tapeCassetteComponent.CurrentPosition)
        {

            if (component.Mode != TapeRecorderMode.Recording)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-tape-recorder-record"),
                    Act = () =>
                    {
                        component.Mode = TapeRecorderMode.Recording;
                        Dirty(uid, component);
                    },
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
                    Priority = 1
                });
            }

            if (component.Mode != TapeRecorderMode.Playing)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Text = Loc.GetString("verb-tape-recorder-playback"),
                    Act = () =>
                    {
                        component.Mode = TapeRecorderMode.Playing;
                        Dirty(uid, component);
                    },
                    Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/playarrow.svg.192dpi.png")),
                    Priority = 2
                });
            }
        }

        //If there is tape to rewind and we are not already rewinding
        if (tapeCassetteComponent.CurrentPosition > float.Epsilon && component.Mode != TapeRecorderMode.Rewinding)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Text = Loc.GetString("verb-tape-recorder-rewind"),
                Act = () =>
                {
                    component.Mode = TapeRecorderMode.Rewinding;
                    Dirty(uid, component);
                },
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/rewindarrow.svg.192dpi.png")),
                Priority = 3
            });
        }
    }

    /// <summary>
    /// Whenever someone speaks within listening range
    /// </summary>
    private void OnListen(EntityUid uid, RecordingTapeRecorderComponent component, ListenEvent args)
    {
        var cassette = _itemSlotsSystem.GetItemOrNull(uid, SlotName);

        if (cassette == null)
            return;


        if (TryComp<TapeCassetteComponent>(cassette, out var tapeCassetteComponent))
        {
            //Handle someone using a voice changer
            var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
            RaiseLocalEvent(args.Source, nameEv);

            //Add a new entry to the tape
            tapeCassetteComponent.RecordedData.Add(new TapeCassetteRecordedMessage(tapeCassetteComponent.CurrentPosition, nameEv.Name, args.Message));
        }
    }

    /// <summary>
    /// Start recording if we are not already recording
    /// </summary>
    protected override bool StartRecording(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!base.StartRecording(tapeRecorder, component, user))
            return false;

        //If we dont know who triggered the sound, play here
        //Otherwise its handled in SharedTapeRecorderSystem for prediction
        if (!user.HasValue)
        {
            _audioSystem.PlayPvs(component.PlaySound, tapeRecorder);
        }

        return true;
    }

    /// <summary>
    /// Stop recording if we are currently recording
    /// </summary>
    protected override bool StopRecording(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!base.StopRecording(tapeRecorder, component, user))
            return false;

        if (!user.HasValue)
        {
            _audioSystem.PlayPvs(component.StopSound, tapeRecorder);
        }

        return true;
    }

    /// <summary>
    /// Start playback if we are not already playing
    /// </summary>
    protected override bool StartPlayback(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!base.StartPlayback(tapeRecorder, component, user))
            return false;

        EnsureComp<VoiceMaskComponent>(tapeRecorder);

        if (!user.HasValue)
        {
            _audioSystem.PlayPvs(component.PlaySound, tapeRecorder);
        }

        return true;
    }

    /// <summary>
    /// Stop playback if we are playing
    /// </summary>
    protected override bool StopPlayback(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!base.StopPlayback(tapeRecorder, component, user))
            return false;

        RemComp<VoiceMaskComponent>(tapeRecorder);

        if (!user.HasValue)
        {
            _audioSystem.PlayPvs(component.StopSound, tapeRecorder);
        }

        return true;
    }

    /// <summary>
    /// Start rewinding the tape if we are not already rewinding
    /// </summary>
    protected override bool StartRewinding(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!base.StartRewinding(tapeRecorder, component, user))
            return false;

        if (!user.HasValue)
        {
            _audioSystem.PlayPvs(component.RewindSound, tapeRecorder);
        }

        return true;
    }

    /// <summary>
    /// Stop rewinding if we are rewinding
    /// </summary>
    protected override bool StopRewinding(EntityUid tapeRecorder, TapeRecorderComponent component, EntityUid? user = null)
    {
        if (!base.StopRewinding(tapeRecorder, component, user))
            return false;

        if (!user.HasValue)
        {
            _audioSystem.PlayPvs(component.StopSound, tapeRecorder);
        }

        return true;
    }
}
