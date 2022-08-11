using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Timing;

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

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TapeRecorderComponent, ChatMessageHeardNearbyEvent>(OnChatMessageHeard);
            SubscribeLocalEvent<TapeRecorderComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<TapeRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var gameTiming = IoCManager.Resolve<IGameTiming>();

            foreach (var tape in EntityManager.EntityQuery<TapeRecorderComponent>())
            {

                tape.AccumulatedTime += frameTime;
                if (tape.Playing)
                {
                    if (tape.TimeStamp > tape.MessageTimeStamps[tape.PlaybackCurrentMessage])
                    {
                        _chat.TrySendInGameICMessage(tape.Owner, tape.MessageList[tape.PlaybackCurrentMessage], InGameICChatType.Speak, false);
                        tape.PlaybackCurrentMessage++;
                    }

                    if (tape.PlaybackCurrentMessage == tape.MessageList.Count)
                        StopPlaying(tape);

                    tape.TimeStamp = tape.AccumulatedTime - tape.TapeStartTime;
                }
            }
        }

        private void OnGetAltVerbs(EntityUid uid, TapeRecorderComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Category = RecorderModes,
                Text = "Play",
                Priority = 5,

                Act = () =>
                {
                    _popupSystem.PopupEntity("pee", args.User, Filter.Entities(args.User));
                },
            });
        }


        public void StartRecording(TapeRecorderComponent component)
        {
            component.Recording = true;
            component.RecordingStartTime = component.AccumulatedTime - component.TimeStamp;
            _popupSystem.PopupEntity("now recording", component.Owner, Filter.Pvs(component.Owner));
        }

        public void StartPlaying(TapeRecorderComponent component)
        {
            component.Recording = false;
            component.Playing = true;
            component.TimeStamp = 0;
            component.PlaybackCurrentMessage = 0;
            component.TapeStartTime = component.AccumulatedTime;
            _popupSystem.PopupEntity("now playing", component.Owner, Filter.Pvs(component.Owner));
        }

        public void StopPlaying(TapeRecorderComponent component)
        {
            component.Playing = false;
            _popupSystem.PopupEntity("stopped playback", component.Owner, Filter.Pvs(component.Owner));
        }

        private void OnUseInHand(EntityUid uid, TapeRecorderComponent component, UseInHandEvent args)
        {

            var currentTime = _gameTiming.CurTime;
            if (currentTime < component.CooldownEnd)
                return;

            component.LastUseTime = currentTime;
            component.CooldownEnd = component.LastUseTime + TimeSpan.FromSeconds(component.CooldownTime);

            if (!component.Recording && !component.Playing)
            {
                StartRecording(component);
                return;
            }

            if (component.Recording && !component.Playing)
            {
                StartPlaying(component);
                return;
            }

            if (component.Playing)
            {
                StopPlaying(component);
            }
        }

        private void OnChatMessageHeard(EntityUid uid, TapeRecorderComponent component, ChatMessageHeardNearbyEvent args)
        {

            if (args.Channel != ChatChannel.Local)
                return;

            if (component.Recording)
            {
                component.MessageList.Add(Name(args.Source) + ": " + args.Message);

                component.MessageTimeStamps.Add(component.AccumulatedTime - component.RecordingStartTime);

                //_chat.TrySendInGameICMessage(component.Owner, (component.AccumulatedTime - component.RecordingStartTime).ToString(), InGameICChatType.Speak, args.HideChat);
            }
        }

        public static VerbCategory RecorderModes = new("Tape Recorder Modes", "/Textures/Interface/VerbIcons/clock.svg.192dpi.png");
    }
}
