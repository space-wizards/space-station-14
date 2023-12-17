using Content.Server.Chat.Systems;
using Content.Server.Speech;
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
    protected override void ReplayMessagesInSegment(EntityUid uid, TapeCassetteComponent component, float segmentStart, float segmentEnd)
    {
        foreach (var messageToBeReplayed in component.RecordedData.Where(x => x.Timestamp > component.CurrentPosition && x.Timestamp <= segmentEnd))
        {
            //Play them
            _chatSystem.TrySendInGameICMessage(uid, messageToBeReplayed.Message, InGameICChatType.Speak, false, nameOverride: messageToBeReplayed.Name);
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
        if (HasComp<PlayingTapeRecorderComponent>(uid) ||
            HasComp<RecordingTapeRecorderComponent>(uid) ||
            HasComp<RewindingTapeRecorderComponent>(uid))
            return;

        //If we have tape capacity remaining
        if (tapeCassetteComponent.MaxCapacity >= tapeCassetteComponent.CurrentPosition)
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

        //If there is tape to rewind
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
}
