using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Notification;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Juke;
using Content.Shared.Power;
using JetBrains.Annotations;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Robust.Server.Audio.Midi;
using Robust.Server.GameObjects;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Juke
{
    [UsedImplicitly]
    public class MidiJukeSystem : SharedMidiJukeSystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IResourceManager _resMan = default!;

        /// <summary>
        /// Dictionary of MIDI file tuples loaded from <see cref="MidiPath"/>, indexed by their short name (currently filename),
        /// and containing the full resource path and track title of the file.
        /// </summary>
        private readonly SortedList<string, (ResourcePath Path, string TrackTitle)> _midiFiles = new();

        private static readonly ResourcePath MidiPath = new("/midis");
        private double _updateAccumulator = 0; //there must be a better way

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MidiJukeComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<MidiJukeComponent, InteractHandEvent>(OnInteractHand);

            var loaded = 0;
            var failed = 0;
            var start = _gameTiming.RealTime;

            var rootDir = _resMan.UserData.RootDir;
            if (rootDir != null && _resMan.UserData.IsDir(MidiPath))
            {
                foreach (var file in _resMan.UserData.GetFiles(MidiPath, false, "*.mid"))
                {
                    var stream = _resMan.UserData.OpenRead(file);
                    try
                    {
                        var midiFile = MidiFile.Read(stream, VirtualMidiPlayer.DefaultReadingSettings);
                        var timedEvents = midiFile.GetTrackChunks().GetTimedEvents();
                        string title = string.Empty;
                        //LINQ ahead, do not tell the authorities
                        if (timedEvents.First(x => x.Event is SequenceTrackNameEvent).Event is SequenceTrackNameEvent
                            trackName)
                        {
                            title = trackName.Text;
                        }
                        _midiFiles.Add(file.Filename, (file, title == string.Empty ? file.Filename : title));
                        loaded++;
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Failed to parse MIDI file {file.Filename}: {e.GetType()}: {e.Message}");
                        failed++;
                    }
                }

                var finish = _gameTiming.RealTime;
                var delta = (finish - start).TotalSeconds;
                Logger.InfoS("MidiJukeSystem",
                    $"Loaded {loaded} MIDI files in {delta} seconds. {failed} files failed to load.");
            }
        }

        private void OnStartup(EntityUid uid, MidiJukeComponent midiJuke, ComponentStartup args)
        {
            //TODO: this is apparently bad and will need to be rewritten once BoundUserInterface becomes ECS friendlier
            var ui = midiJuke.Owner.GetUIOrNull(MidiJukeUiKey.Key);
            if (ui != null)
            {
                ui.OnReceiveMessage += msg => OnMidiJukeUiMessage(uid, midiJuke, msg);
                ui.SetState(GetState(midiJuke));
            }
        }

        private void DirtyUI(EntityUid uid)
        {
            if (!ComponentManager.TryGetComponent(uid, out MidiJukeComponent midiJuke)
                || !ComponentManager.TryGetComponent(uid, out ServerUserInterfaceComponent userInterfaceComponent)
                || !userInterfaceComponent.TryGetBoundUserInterface(MidiJukeUiKey.Key, out var ui))
                return;

            ui.SetState(GetState(midiJuke));
            UpdateTimestamp(uid, midiJuke.PlaybackStatus == MidiJukePlaybackStatus.Stop);
        }

        private MidiJukeBoundUserInterfaceState GetState(MidiJukeComponent component)
        {
            var currentSong = component.MidiFileName;
            string currentSongTitle = currentSong;
            if (_midiFiles.TryGetValue(currentSong, out var tuple))
            {
                currentSongTitle = tuple.TrackTitle;
            }
            return new MidiJukeBoundUserInterfaceState(component.PlaybackStatus, component.Loop,
                currentSong, currentSongTitle);
        }

        private void UpdateSongList(EntityUid uid)
        {
            if (!ComponentManager.TryGetComponent(uid, out MidiJukeComponent midiJuke)
                || !ComponentManager.TryGetComponent(uid, out ServerUserInterfaceComponent userInterfaceComponent)
                || !userInterfaceComponent.TryGetBoundUserInterface(MidiJukeUiKey.Key, out var ui))
                return;
            ui.SendMessage(new MidiJukeSongListMessage(_midiFiles.Keys.ToList()));
        }

        private void UpdateTimestamp(EntityUid uid, bool blank = false)
        {
            if (!ComponentManager.TryGetComponent(uid, out MidiJukeComponent midiJuke)
                || !ComponentManager.TryGetComponent(uid, out ServerUserInterfaceComponent userInterfaceComponent)
                || !userInterfaceComponent.TryGetBoundUserInterface(MidiJukeUiKey.Key, out var ui))
                return;

            int? elapsed, duration;
            if (blank)
            {
                elapsed = duration = null;
            }
            else
            {
                elapsed = midiJuke.MidiPlayer?.CurrentTimeSeconds;
                duration = midiJuke.MidiPlayer?.DurationSeconds;
            }
            ui.SendMessage(new MidiJukeTimestampMessage(elapsed, duration));
        }

        private void OnMidiJukeUiMessage(EntityUid uid, MidiJukeComponent component, ServerBoundUserInterfaceMessage msg)
        {
            var entity = msg.Session.AttachedEntity;
            if (entity == null
                || !Get<ActionBlockerSystem>().CanInteract(entity)
                || !Get<ActionBlockerSystem>().CanUse(entity))
                return;

            switch (msg.Message)
            {
                case MidiJukePlayMessage:
                    Play(component);
                    break;
                case MidiJukePauseMessage:
                    Pause(component);
                    break;
                case MidiJukeStopMessage:
                    Stop(component);
                    break;
                case MidiJukeSkipMessage:
                    ShuffleSong(component);
                    break;
                case MidiJukeLoopMessage loopMsg:
                    SetLoop(component, loopMsg.Loop);
                    break;
                case MidiJukeSongSelectMessage songMsg:
                    Stop(component);
                    OpenMidiFile(component, songMsg.Song);
                    Play(component);
                    break;
                case MidiJukeSongListRequestMessage:
                    UpdateSongList(uid);
                    break;
            }

            component.Dirty();
            DirtyUI(component.Owner.Uid);
        }

        private void OnInteractHand(EntityUid uid, MidiJukeComponent component, InteractHandEvent args)
        {
            if (!args.User.TryGetComponent(out ActorComponent? actor)) return;
            component.Owner.GetUIOrNull(MidiJukeUiKey.Key)?.Open(actor.PlayerSession);
            args.Handled = true;
        }

        public override void Update(float frameTime)
        {
            //We only need to update the timestamp ~once a second instead of every single tick.
            _updateAccumulator += frameTime;
            var updateTimestamp = false;
            if (_updateAccumulator >= 1)
            {
                updateTimestamp = true;
                _updateAccumulator = 0;
            }

            foreach (var component in ComponentManager.EntityQuery<MidiJukeComponent>(true))
            {
                if (!component.Playing || component.MidiPlayer == null) continue;
                var midiEvents = component.MidiPlayer?.TickClockAndPopEventBuffer();
                if (midiEvents == null) continue;
                var uid = component.Owner.Uid;
                //TODO: only send this in PVS -- requires some way to figure how to stop ghost notes when client misses
                //NoteOff event when outside of PVS.
                //RaiseNetworkEvent(new MidiJukeMidiEventsEvent(uid, midiEvents.ToArray()), Filter.Pvs(component.Owner));
                RaiseNetworkEvent(new MidiJukeMidiEventsEvent(uid, midiEvents.ToArray()), Filter.Broadcast());
                if (updateTimestamp)
                    UpdateTimestamp(uid);
            }
        }

        private void SetAppearance(EntityUid uid, MidiJukeVisualState state)
        {
            if (!ComponentManager.TryGetComponent(uid, out AppearanceComponent appearanceComponent))
            {
                return;
            }

            appearanceComponent.SetData(PowerDeviceVisuals.Powered, state);
        }

        /// <summary>
        /// Loads a MIDI file into the component's MIDI player.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="filename"></param>
        private void OpenMidiFile(MidiJukeComponent component, string filename)
        {
            var filepath = _midiFiles[filename].Path;
            if (!_resMan.UserData.Exists(filepath))
            {
                Logger.ErrorS("MidiJukeSystem", $"Tried to open invalid MIDI file: {filepath}");
                return;
            }

            var stream = _resMan.UserData.OpenRead(filepath);
            component.MidiPlayer?.Dispose();
            component.MidiPlayer = VirtualMidiPlayer.FromStream(stream);
            if (component.MidiPlayer != null)
            {
                component.MidiFileName = filename;
                component.MidiPlayer.Finished += (sender, eventArgs) => OnPlaybackFinished(component); //LOW KEY WORRIED ABOUT THE MEMORY SAFETY OF THIS
                component.MidiPlayer.Loop = component.Loop;

                //We need to keep track of some MIDI events that can affect playback (program changes, note events)
                //so we can resync clients who move in/out of range during playback.
                //TODO: uncomment this when (if) we make midi events not global
                // component.MidiPlayer.EventPlayed += (sender, eventArgs) =>
                // {
                //     if (eventArgs.Event is ProgramChangeEvent programChangeEvent)
                //     {
                //         component.ChannelPrograms[programChangeEvent.Channel] = programChangeEvent.ProgramNumber;
                //         //component.Dirty(); //Probably don't actually need to call this since we only want this updated by PVS
                //     }
                // };
            }
        }

        private void OnPlaybackFinished(MidiJukeComponent component)
        {
            Logger.Debug("Playback finished, shuffling song.");
            component.Owner.SpawnTimer(1000, () =>
            {
                var nextSong = ShuffleSong(component);
                DirtyUI(component.Owner.Uid);
                if (nextSong != null)
                    component.Owner.PopupMessageEveryone(Loc.GetString("comp-juke-midi-now-playing-message", ("song", nextSong)));
            });
        }

        /// <summary>
        /// Starts playback on the juke.
        /// </summary>
        /// <param name="component"></param>
        private void Play(MidiJukeComponent component)
        {
            if (!component.Playing && component.MidiPlayer != null)
            {
                component.MidiPlayer.Start();
                component.PlaybackStatus = MidiJukePlaybackStatus.Play;
                //RaiseNetworkEvent(new MidiJukePlayEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
                RaiseNetworkEvent(new MidiJukePlayEvent(component.Owner.Uid), Filter.Broadcast());
            }
        }

        /// <summary>
        /// Pauses playback, maintaining position in the file.
        /// </summary>
        /// <param name="component"></param>
        private void Pause(MidiJukeComponent component)
        {
            if (component.Playing && component.MidiPlayer != null)
            {
                component.MidiPlayer.Stop();
                component.PlaybackStatus = MidiJukePlaybackStatus.Pause;
                component.MidiPlayer.FlushEventBuffer();
                //RaiseNetworkEvent(new MidiJukePauseEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
                RaiseNetworkEvent(new MidiJukePauseEvent(component.Owner.Uid), Filter.Broadcast());
            }
        }

        /// <summary>
        /// Stops playback, resetting position in the file to the beginning.
        /// </summary>
        /// <param name="component"></param>
        private void Stop(MidiJukeComponent component)
        {
            if (component.MidiPlayer != null)
            {
                component.MidiPlayer.Stop();
                component.PlaybackStatus = MidiJukePlaybackStatus.Stop;
                component.MidiPlayer.FlushEventBuffer();
                //RaiseNetworkEvent(new MidiJukeStopEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
                RaiseNetworkEvent(new MidiJukeStopEvent(component.Owner.Uid), Filter.Broadcast());
                component.MidiPlayer.MoveToStart();
            }
        }

        /// <summary>
        /// Picks a random song to play on the component.
        /// </summary>
        /// <param name="component"></param>
        /// <returns>The file name of the song picked, or null if we failed to pick anything.</returns>
        private string? ShuffleSong(MidiJukeComponent component)
        {
            if (component.MidiPlayer != null)
            {
                component.MidiPlayer.Dispose();
                component.MidiPlayer = null;
            }

            component.PlaybackStatus = MidiJukePlaybackStatus.Stop;
            var nextSong = _robustRandom.Pick(_midiFiles.Keys.ToList());
            OpenMidiFile(component, nextSong);
            //RaiseNetworkEvent(new MidiJukePlaybackFinishedEvent(component.Owner.Uid), Filter.Pvs(component.Owner));
            RaiseNetworkEvent(new MidiJukePlaybackFinishedEvent(component.Owner.Uid), Filter.Broadcast());
            if (component.MidiPlayer == null)
            {
                return null;
            }
            Play(component);
            return nextSong;
        }

        private void SetLoop(MidiJukeComponent component, bool loop)
        {
            component.Loop = loop;
            if (component.MidiPlayer != null)
            {
                component.MidiPlayer.Loop = loop;
            }
        }
    }
}
