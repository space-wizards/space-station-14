using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.TapeRecorder;

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

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TapeRecorderComponent, ChatMessageHeardNearbyEvent>(OnChatMessageHeard);
            SubscribeLocalEvent<TapeRecorderComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<TapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
            SubscribeLocalEvent<TapeRecorderComponent, ExaminedEvent>(OnExamined);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var tape in EntityManager.EntityQuery<TapeRecorderComponent>())
            {

                tape.AccumulatedTime += frameTime;

                //Handle tape playback
                if (tape.Enabled && tape.CurrentMode == TapeRecorderState.Play)
                {

                    if (tape.PlaybackCurrentMessage >= tape.RecordedMessages.Count)
                    {
                        StopTape(tape);
                        return;
                    }

                    if (tape.TimeStamp > tape.RecordedMessages[tape.PlaybackCurrentMessage].MessageTimeStamp)
                    {
                        _chat.TrySendInGameICMessage(tape.Owner, tape.RecordedMessages[tape.PlaybackCurrentMessage].Message, InGameICChatType.Speak, false);
                        tape.PlaybackCurrentMessage++;
                    }

                    tape.TimeStamp += frameTime;
                }

                if (tape.Enabled && tape.CurrentMode == TapeRecorderState.Rewind)
                {
                    tape.TimeStamp -= frameTime;

                    if (tape.TimeStamp <= 0)
                    {
                        tape.TimeStamp = 0;
                        tape.PlaybackCurrentMessage = 0;
                        StopTape(tape);
                        return;
                    }

                }

            }
        }

        public void StartRecording(TapeRecorderComponent component)
        {
            component.RecordingStartTime = component.AccumulatedTime - component.TimeStamp;
            _popupSystem.PopupEntity("The tape recorder starts recording", component.Owner, Filter.Pvs(component.Owner));
            _audioSystem.PlayPvs(component.StartSound, component.Owner);
        }

        public void StartPlaying(TapeRecorderComponent component)
        {
            int test = 0;

            for (int i = 0; i < component.RecordedMessages.Count; i++)
            {
                if (component.RecordedMessages[i].MessageTimeStamp < component.TimeStamp)
                    test = i + 1;
            } //finding out which message we're on

            _popupSystem.PopupEntity(test.ToString(), component.Owner, Filter.Pvs(component.Owner));

            //component.TimeStamp = 0;
            component.PlaybackCurrentMessage = test;
            //component.TapeStartTime = component.AccumulatedTime;
            _popupSystem.PopupEntity("The tape recorder starts playback", component.Owner, Filter.Pvs(component.Owner));
            _audioSystem.PlayPvs(component.StartSound, component.Owner);
        }

        public void StopTape(TapeRecorderComponent component)
        {
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
            switch (component.CurrentMode)
            {
                case TapeRecorderState.Play:
                    StartPlaying(component);
                    break;
                case TapeRecorderState.Record:
                    StartRecording(component);
                    break;
                case TapeRecorderState.Rewind:
                    break;
                case TapeRecorderState.Idle:
                    break;
            }
        }

        private void OnChatMessageHeard(EntityUid uid, TapeRecorderComponent component, ChatMessageHeardNearbyEvent args)
        {

            if (args.Channel != ChatChannel.Local)
                return;

            component.TimeStamp = (component.AccumulatedTime - component.RecordingStartTime);

            if (component.CurrentMode == TapeRecorderState.Record && component.Enabled)
            {
                component.RecordedMessages.Add((component.TimeStamp, TimeSpan.FromSeconds(component.TimeStamp).ToString("mm\\:ss") + " : " + Name(args.Source) + ": " + args.Message));
                //_chat.TrySendInGameICMessage(component.Owner, (component.AccumulatedTime - component.RecordingStartTime).ToString(), InGameICChatType.Speak, args.HideChat);
            }

        }

        private void OnExamined(EntityUid uid, TapeRecorderComponent component, ExaminedEvent args)
        {
            if (!args.IsInDetailsRange)
                return;

            args.PushMarkup(TimeSpan.FromSeconds(component.TimeStamp).ToString("mm\\:ss") + " / " + (TimeSpan.FromSeconds(component.TapeMaxTime)));
        }

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
