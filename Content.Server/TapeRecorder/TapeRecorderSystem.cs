using System.Linq;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.TapeRecorder;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.TapeRecorder
{
    /// <summary>
    /// This handles...
    /// </summary>
    public sealed class TapeRecorderSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TapeRecorderComponent, ChatMessageHeardNearbyEvent>(OnChatMessageHeard);
            SubscribeLocalEvent<TapeRecorderComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<TapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
            SubscribeLocalEvent<TapeRecorderComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<TapeRecorderComponent, ItemSlotChangedEvent>(OnItemSlotChanged);
        }

        private void OnItemSlotChanged(EntityUid uid, TapeRecorderComponent component, ref ItemSlotChangedEvent args)
        {
            if (component.Enabled && component.CurrentMode == TapeRecorderState.Record)
                FlushBufferToMemory(component); //incase we rip it out while recording

            if (!_containerSystem.TryGetContainer(uid, "cassette_tape", out var container) || container is not ContainerSlot slot)
                return;

            if (!TryComp<CassetteTapeComponent>(slot.ContainedEntity, out var cassetteTapeComponent))
            {
                component.CurrentMode = TapeRecorderState.Empty;
                StopTape(component);
                component.InsertedTape = null;
                return;
            }

            component.CurrentMode = TapeRecorderState.Idle;
            component.InsertedTape = cassetteTapeComponent;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var tapeRecorder in EntityManager.EntityQuery<TapeRecorderComponent>())
            {

                tapeRecorder.AccumulatedTime += frameTime;

                if (tapeRecorder.InsertedTape == null)
                    return;

                //stop player if tape is at end
                if (tapeRecorder.Enabled && tapeRecorder.InsertedTape.TimeStamp >= tapeRecorder.InsertedTape.TapeMaxTime && tapeRecorder.CurrentMode != TapeRecorderState.Rewind)
                {
                    StopTape(tapeRecorder);
                    tapeRecorder.CurrentMode = TapeRecorderState.Rewind; // go into rewind mode once at end of tape
                }

                //Handle tape playback
                if (tapeRecorder.Enabled && tapeRecorder.CurrentMode == TapeRecorderState.Play)
                {

                    if (tapeRecorder.CurrentMessageIndex >= tapeRecorder.InsertedTape.RecordedMessages.Count || tapeRecorder.InsertedTape.TimeStamp >= tapeRecorder.InsertedTape.TapeMaxTime)
                    {
                        StopTape(tapeRecorder);
                        tapeRecorder.CurrentMode = TapeRecorderState.Rewind; // go into record mode once at at recorded data
                        return;
                    }

                    if (tapeRecorder.InsertedTape.TimeStamp > tapeRecorder.InsertedTape.RecordedMessages[tapeRecorder.CurrentMessageIndex].MessageTimeStamp)
                    {
                        _chat.TrySendInGameICMessage(tapeRecorder.Owner, tapeRecorder.InsertedTape.RecordedMessages[tapeRecorder.CurrentMessageIndex].Message, InGameICChatType.Speak, false);
                        tapeRecorder.CurrentMessageIndex++;
                    }

                    tapeRecorder.InsertedTape.TimeStamp += frameTime;
                }

                //Tape rewinding (maybe) fast-forwarding later
                if (!tapeRecorder.Enabled || tapeRecorder.CurrentMode != TapeRecorderState.Rewind)
                    continue;

                tapeRecorder.InsertedTape.TimeStamp -= frameTime * 3;

                if (tapeRecorder.InsertedTape.TimeStamp <= 0) //stop rewinding when we get to the beginning of the tape
                {
                    tapeRecorder.InsertedTape.TimeStamp = 0;
                    tapeRecorder.CurrentMessageIndex = 0;
                    StopTape(tapeRecorder);
                    tapeRecorder.CurrentMode = TapeRecorderState.Play; // go into play mode once finished rewinding
                    return;
                }

            }
        }

        public void StartRecording(TapeRecorderComponent component)
        {
            if (component.InsertedTape == null)
                return;

            component.RecordingStartTime = component.AccumulatedTime - component.InsertedTape.TimeStamp;
            component.RecordingStartTimestamp = component.InsertedTape.TimeStamp;

            _popupSystem.PopupEntity("The tape recorder starts recording", component.Owner, Filter.Pvs(component.Owner));
            _audioSystem.PlayPvs(component.StartSound, component.Owner);
        }

        public void StartPlaying(TapeRecorderComponent component)
        {
            component.CurrentMessageIndex = GetTapeIndex(component);
            _popupSystem.PopupEntity("The tape recorder starts playback", component.Owner, Filter.Pvs(component.Owner));
            _audioSystem.PlayPvs(component.StartSound, component.Owner);
        }

        private static int GetTapeIndex(TapeRecorderComponent component) // returns the index of the message we are on, based on where the tape is
        {
            if (component.InsertedTape == null)
                return 0;

            if (component.InsertedTape.RecordedMessages.Count == 0)
                return 0;

            //Find the index with the closest timestamp to component.InsertedTape.TimeStamp (using this to keep track of what message we're on because if we do it while playing it might be expensive)
            var closest = component.InsertedTape.RecordedMessages.Select((x, i) => new { Index = i, TimeStamp = x.MessageTimeStamp }).OrderBy(x => Math.Abs(x.TimeStamp - component.InsertedTape.TimeStamp)).First();
            return closest.Index;
        }

        private static void FlushBufferToMemory(TapeRecorderComponent component)
        {
            if (component.InsertedTape == null)
                return;

            component.InsertedTape.TimeStamp = (component.AccumulatedTime - component.RecordingStartTime);

            //Clear the recorded messages between the start and end of our recording timestamps, since we're overwriting this part of the tape
            component.InsertedTape.RecordedMessages.RemoveAll(x => x.MessageTimeStamp > component.RecordingStartTimestamp && x.MessageTimeStamp < component.InsertedTape.TimeStamp);
            component.InsertedTape.RecordedMessages.AddRange(component.RecordedMessageBuffer);

            //Clear the buffer
            component.RecordedMessageBuffer.Clear();

            //sort the list by timestamp
            component.InsertedTape.RecordedMessages.Sort((x, y) => x.MessageTimeStamp.CompareTo(y.MessageTimeStamp));

            }

        public void StopTape(TapeRecorderComponent component)
        {
            if (component.CurrentMode == TapeRecorderState.Record && component.Enabled)
                FlushBufferToMemory(component);

            if (!component.Enabled)
                return;

            _audioSystem.PlayPvs(component.StopSound, component.Owner);
            component.Enabled = false;
            UpdateAppearance(component);

        }

        private void UpdateAppearance(TapeRecorderComponent component)
        {
            if (!TryComp<AppearanceComponent>(component.Owner, out var appearance))
                return;

            if (!component.Enabled)
            {
                appearance.SetData(TapeRecorderVisuals.Status, TapeRecorderState.Idle);
                return;
            }
            appearance.SetData(TapeRecorderVisuals.Status, component.CurrentMode);
        }

        private void OnUseInHand(EntityUid uid, TapeRecorderComponent component, UseInHandEvent args)
        {
            //Use in hand cooldown
            var currentTime = _gameTiming.CurTime;
            if (currentTime < component.CooldownEnd)
                return;
            component.LastUseTime = currentTime;
            component.CooldownEnd = component.LastUseTime + TimeSpan.FromSeconds(component.CooldownTime);



            if (component.Enabled)
            {
                StopTape(component);
                return;
            }

            if (!component.Enabled)
                component.Enabled = true;
            UpdateAppearance(component);

            switch (component.CurrentMode) //idk how else to do this
            {
                case TapeRecorderState.Play:
                    StartPlaying(component);
                    break;
                case TapeRecorderState.Record:
                    StartRecording(component);
                    break;
                case TapeRecorderState.Rewind:
                    _audioSystem.PlayPvs(component.StartSound, component.Owner);
                    break;
                case TapeRecorderState.Idle:
                    break;
            }
        }

        private void OnChatMessageHeard(EntityUid uid, TapeRecorderComponent component, ChatMessageHeardNearbyEvent args)
        {
            if (component.InsertedTape == null)
                return;

            if (args.Channel != ChatChannel.Local) //filter out messages that aren't local chat (whispering should be picked up by the recorder, neither should emotes)
                return;

            component.InsertedTape.TimeStamp = (component.AccumulatedTime - component.RecordingStartTime);

            //Record messages to the BUFFER (so we can overwrite messages that we are recording over)
            if (component.CurrentMode == TapeRecorderState.Record && component.Enabled)
            {
                component.RecordedMessageBuffer.Add((component.InsertedTape.TimeStamp, TimeSpan.FromSeconds(component.InsertedTape.TimeStamp).ToString("mm\\:ss") + " : " + Name(args.Source) + ": " + args.Message));
            }
        }

        private void OnExamined(EntityUid uid, TapeRecorderComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange || component.InsertedTape == null)
                return;

            args.PushMarkup(TimeSpan.FromSeconds(component.InsertedTape.TimeStamp).ToString("mm\\:ss") + " / " + (TimeSpan.FromSeconds(component.InsertedTape.TapeMaxTime)));
        }


        //the verb sewer
        private void OnGetAltVerbs(EntityUid uid, TapeRecorderComponent component, GetVerbsEvent<AlternativeVerb> args)
        {

            if (component.CurrentMode != TapeRecorderState.Play)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Category = RecorderModes,
                    Text = "Play",
                    Priority = 5,

                    Act = () =>
                    {
                        StopTape(component);
                        _popupSystem.PopupEntity("Play mode", component.Owner, Filter.Pvs(component.Owner));
                        component.CurrentMode = TapeRecorderState.Play;
                    },
                });
            }
            if (component.CurrentMode != TapeRecorderState.Record)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Category = RecorderModes,
                    Text = "Record",
                    Priority = 5,

                    Act = () =>
                    {
                        StopTape(component);
                        _popupSystem.PopupEntity("Record mode", component.Owner, Filter.Pvs(component.Owner));
                        component.CurrentMode = TapeRecorderState.Record;
                    },
                });
            }
            if (component.CurrentMode != TapeRecorderState.Rewind)
            {
                args.Verbs.Add(new AlternativeVerb()
                {
                    Category = RecorderModes,
                    Text = "Rewind",
                    Priority = 5,

                    Act = () =>
                    {
                        StopTape(component);
                        _popupSystem.PopupEntity("Rewind mode", component.Owner, Filter.Pvs(component.Owner));
                        component.CurrentMode = TapeRecorderState.Rewind;
                    },
                });
            }
        }

        public static VerbCategory RecorderModes = new("Tape Recorder Modes", "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");
    }
}
