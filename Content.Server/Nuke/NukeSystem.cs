using Content.Server.AlertLevel;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Server.Coordinates.Helpers;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.UserInterface;
using Content.Shared.Audio;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Nuke;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Nuke
{
    public sealed class NukeSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly ServerGlobalSoundSystem _soundSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        /// <summary>
        ///     Used to calculate when the nuke song should start playing for maximum kino with the nuke sfx
        /// </summary>
        private const float NukeSongLength = 60f + 51.6f;

        /// <summary>
        ///     Time to leave between the nuke song and the nuke alarm playing.
        /// </summary>
        private const float NukeSongBuffer = 1.5f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NukeComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<NukeComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<NukeComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<NukeComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
            SubscribeLocalEvent<NukeComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);

            // anchoring logic
            SubscribeLocalEvent<NukeComponent, AnchorAttemptEvent>(OnAnchorAttempt);
            SubscribeLocalEvent<NukeComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            // Shouldn't need re-anchoring.
            SubscribeLocalEvent<NukeComponent, AnchorStateChangedEvent>(OnAnchorChanged);

            // ui events
            SubscribeLocalEvent<NukeComponent, NukeAnchorMessage>(OnAnchorButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeArmedMessage>(OnArmButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeKeypadMessage>(OnKeypadButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeKeypadClearMessage>(OnClearButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeKeypadEnterMessage>(OnEnterButtonPressed);

            // Doafter events
            SubscribeLocalEvent<NukeComponent, NukeDisarmSuccessEvent>(OnDisarmSuccess);
            SubscribeLocalEvent<NukeComponent, NukeDisarmCancelledEvent>(OnDisarmCancelled);
        }

        private void OnInit(EntityUid uid, NukeComponent component, ComponentInit args)
        {
            component.RemainingTime = component.Timer;
            _itemSlots.AddItemSlot(uid, SharedNukeComponent.NukeDiskSlotId, component.DiskSlot);

            UpdateStatus(uid, component);
            UpdateUserInterface(uid, component);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQuery<NukeComponent>();
            foreach (var nuke in query)
            {
                switch (nuke.Status)
                {
                    case NukeStatus.ARMED:
                        TickTimer(nuke.Owner, frameTime, nuke);
                        break;
                    case NukeStatus.COOLDOWN:
                        TickCooldown(nuke.Owner, frameTime, nuke);
                        break;
                }
            }
        }

        private void OnMapInit(EntityUid uid, NukeComponent nuke, MapInitEvent args)
        {
            var originStation = _stationSystem.GetOwningStation(uid);

            if (originStation != null)
                nuke.OriginStation = originStation;

            else
            {
                var transform = Transform(uid);
                nuke.OriginMapGrid = (transform.MapID, transform.GridUid);
            }

            nuke.Code = GenerateRandomNumberString(nuke.CodeLength);
        }

        private void OnRemove(EntityUid uid, NukeComponent component, ComponentRemove args)
        {
            _itemSlots.RemoveItemSlot(uid, component.DiskSlot);
        }

        private void OnItemSlotChanged(EntityUid uid, NukeComponent component, ContainerModifiedMessage args)
        {
            if (!component.Initialized)
                return;

            if (args.Container.ID != component.DiskSlot.ID)
                return;

            UpdateStatus(uid, component);
            UpdateUserInterface(uid, component);
        }

        #region Anchor

        private void OnAnchorAttempt(EntityUid uid, NukeComponent component, AnchorAttemptEvent args)
        {
            CheckAnchorAttempt(uid, component, args);
        }

        private void OnUnanchorAttempt(EntityUid uid, NukeComponent component, UnanchorAttemptEvent args)
        {
            CheckAnchorAttempt(uid, component, args);
        }

        private void CheckAnchorAttempt(EntityUid uid, NukeComponent component, BaseAnchoredAttemptEvent args)
        {
            // cancel any anchor attempt if armed
            if (component.Status == NukeStatus.ARMED)
            {
                var msg = Loc.GetString("nuke-component-cant-anchor");
                _popups.PopupEntity(msg, uid, args.User);

                args.Cancel();
            }
        }

        private void OnAnchorChanged(EntityUid uid, NukeComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateUserInterface(uid, component);

            if (args.Anchored == false && component.Status == NukeStatus.ARMED && component.RemainingTime > component.DisarmDoafterLength)
            {
                // yes, this means technically if you can find a way to unanchor the nuke, you can disarm it
                // without the doafter. but that takes some effort, and it won't allow you to disarm a nuke that can't be disarmed by the doafter.
                DisarmBomb(uid, component);
            }
        }

        #endregion

        #region UI Events

        private async void OnAnchorButtonPressed(EntityUid uid, NukeComponent component, NukeAnchorMessage args)
        {
            if (!component.DiskSlot.HasItem)
                return;

            if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform))
                return;

            // manually set transform anchor (bypassing anchorable)
            // todo: it will break pullable system
            transform.Coordinates = transform.Coordinates.SnapToGrid();
            transform.Anchored = !transform.Anchored;

            UpdateUserInterface(uid, component);
        }

        private void OnEnterButtonPressed(EntityUid uid, NukeComponent component, NukeKeypadEnterMessage args)
        {
            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            UpdateStatus(uid, component);
            UpdateUserInterface(uid, component);
        }

        private void OnKeypadButtonPressed(EntityUid uid, NukeComponent component, NukeKeypadMessage args)
        {
            PlayNukeKeypadSound(uid, args.Value, component);

            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            if (component.EnteredCode.Length >= component.CodeLength)
                return;

            component.EnteredCode += args.Value.ToString();
            UpdateUserInterface(uid, component);
        }

        private void OnClearButtonPressed(EntityUid uid, NukeComponent component, NukeKeypadClearMessage args)
        {
            PlaySound(uid, component.KeypadPressSound, 0f, component);

            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            component.EnteredCode = "";
            UpdateUserInterface(uid, component);
        }

        private void OnArmButtonPressed(EntityUid uid, NukeComponent component, NukeArmedMessage args)
        {
            if (!component.DiskSlot.HasItem)
                return;

            if (component.Status == NukeStatus.AWAIT_ARM && Transform(uid).Anchored)
                ArmBomb(uid, component);

            else
            {
                if (args.Session.AttachedEntity is not { } user)
                    return;

                DisarmBombDoafter(uid, user, component);
            }
        }

        #endregion

        #region Doafter Events

        private void OnDisarmSuccess(EntityUid uid, NukeComponent component, NukeDisarmSuccessEvent args)
        {
            component.DisarmCancelToken = null;
            DisarmBomb(uid, component);
        }

        private void OnDisarmCancelled(EntityUid uid, NukeComponent component, NukeDisarmCancelledEvent args)
        {
            component.DisarmCancelToken = null;
        }

        #endregion

        private void TickCooldown(EntityUid uid, float frameTime, NukeComponent? nuke = null)
        {
            if (!Resolve(uid, ref nuke))
                return;

            nuke.CooldownTime -= frameTime;
            if (nuke.CooldownTime <= 0)
            {
                // reset nuke to default state
                nuke.CooldownTime = 0;
                nuke.Status = NukeStatus.AWAIT_ARM;
                UpdateStatus(uid, nuke);
            }

            UpdateUserInterface(nuke.Owner, nuke);
        }

        private void TickTimer(EntityUid uid, float frameTime, NukeComponent? nuke = null)
        {
            if (!Resolve(uid, ref nuke))
                return;

            nuke.RemainingTime -= frameTime;

            // Start playing the nuke event song so that it ends a couple seconds before the alert sound
            // should play
            if (nuke.RemainingTime <= NukeSongLength + nuke.AlertSoundTime + NukeSongBuffer && !nuke.PlayedNukeSong)
            {
                _soundSystem.DispatchStationEventMusic(uid, nuke.ArmMusic, StationEventMusicType.Nuke);
                nuke.PlayedNukeSong = true;
            }

            // play alert sound if time is running out
            if (nuke.RemainingTime <= nuke.AlertSoundTime && !nuke.PlayedAlertSound)
            {
                nuke.AlertAudioStream = SoundSystem.Play(nuke.AlertSound.GetSound(), Filter.Broadcast());
                _soundSystem.StopStationEventMusic(uid, StationEventMusicType.Nuke);
                nuke.PlayedAlertSound = true;
            }

            if (nuke.RemainingTime <= 0)
            {
                nuke.RemainingTime = 0;
                ActivateBomb(uid, nuke);
            }

            else
                UpdateUserInterface(uid, nuke);
        }

        private void UpdateStatus(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            switch (component.Status)
            {
                case NukeStatus.AWAIT_DISK:
                    if (component.DiskSlot.HasItem)
                        component.Status = NukeStatus.AWAIT_CODE;
                    break;
                case NukeStatus.AWAIT_CODE:
                {
                    if (!component.DiskSlot.HasItem)
                    {
                        component.Status = NukeStatus.AWAIT_DISK;
                        component.EnteredCode = "";
                        break;
                    }

                    // var isValid = _codes.IsCodeValid(uid, component.EnteredCode);
                    if (component.EnteredCode == component.Code)
                    {
                        component.Status = NukeStatus.AWAIT_ARM;
                        component.RemainingTime = component.Timer;
                        PlaySound(uid, component.AccessGrantedSound, 0, component);
                    }
                    else
                    {
                        component.EnteredCode = "";
                        PlaySound(uid, component.AccessDeniedSound, 0, component);
                    }

                    break;
                }
                case NukeStatus.AWAIT_ARM:
                    // do nothing, wait for arm button to be pressed
                    break;
                case NukeStatus.ARMED:
                    // do nothing, wait for arm button to be unpressed
                    break;
            }
        }

        private void UpdateUserInterface(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(NukeUiKey.Key);
            if (ui == null)
                return;

            var anchored = false;
            if (EntityManager.TryGetComponent(uid, out TransformComponent? transform))
                anchored = transform.Anchored;

            var allowArm = component.DiskSlot.HasItem &&
                           (component.Status == NukeStatus.AWAIT_ARM ||
                            component.Status == NukeStatus.ARMED);

            var state = new NukeUiState()
            {
                Status = component.Status,
                RemainingTime = (int) component.RemainingTime,
                DiskInserted = component.DiskSlot.HasItem,
                IsAnchored = anchored,
                AllowArm = allowArm,
                EnteredCodeLength = component.EnteredCode.Length,
                MaxCodeLength = component.CodeLength,
                CooldownTime = (int) component.CooldownTime
            };

            ui.SetState(state);
        }

        private void PlayNukeKeypadSound(EntityUid uid, int number, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            // This is a C mixolydian blues scale.
            // 1 2 3    C D Eb
            // 4 5 6    E F F#
            // 7 8 9    G A Bb
            var semitoneShift = number switch
            {
                1 => 0,
                2 => 2,
                3 => 3,
                4 => 4,
                5 => 5,
                6 => 6,
                7 => 7,
                8 => 9,
                9 => 10,
                0 => component.LastPlayedKeypadSemitones + 12,
                _ => 0
            };

            // Don't double-dip on the octave shifting
            component.LastPlayedKeypadSemitones = number == 0 ? component.LastPlayedKeypadSemitones : semitoneShift;

            SoundSystem.Play(component.KeypadPressSound.GetSound(), Filter.Pvs(uid), uid,
                AudioHelpers.ShiftSemitone(semitoneShift).WithVolume(-5f));
        }

        private void PlaySound(EntityUid uid, SoundSpecifier sound, float varyPitch = 0f,
            NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            SoundSystem.Play(sound.GetSound(),
                Filter.Pvs(uid), uid, AudioHelpers.WithVariation(varyPitch).WithVolume(-5f));
        }

        public string GenerateRandomNumberString(int length)
        {
            var ret = "";
            for (var i = 0; i < length; i++)
            {
                var c = (char) _random.Next('0', '9' + 1);
                ret += c;
            }

            return ret;
        }

        #region Public API

        /// <summary>
        ///     Force a nuclear bomb to start a countdown timer
        /// </summary>
        public void ArmBomb(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Status == NukeStatus.ARMED)
                return;

            var stationUid = _stationSystem.GetOwningStation(uid);
            // The nuke may not be on a station, so it's more important to just
            // let people know that a nuclear bomb was armed in their vicinity instead.
            // Otherwise, you could set every station to whatever AlertLevelOnActivate is.
            if (stationUid != null)
                _alertLevel.SetLevel(stationUid.Value, component.AlertLevelOnActivate, true, true, true, true);

            var nukeXform = Transform(uid);
            var pos =  nukeXform.MapPosition;
            var x = (int) pos.X;
            var y = (int) pos.Y;
            var posText = $"({x}, {y})";

            // warn a crew
            var announcement = Loc.GetString("nuke-component-announcement-armed",
                ("time", (int) component.RemainingTime), ("position", posText));
            var sender = Loc.GetString("nuke-component-announcement-sender");
            _chatSystem.DispatchStationAnnouncement(uid, announcement, sender, false, null, Color.Red);

            NukeArmedAudio(component);

            _itemSlots.SetLock(uid, component.DiskSlot, true);
            nukeXform.Anchored = true;
            component.Status = NukeStatus.ARMED;
            UpdateUserInterface(uid, component);
        }

        /// <summary>
        ///     Stop nuclear bomb timer
        /// </summary>
        public void DisarmBomb(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Status != NukeStatus.ARMED)
                return;

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
                _alertLevel.SetLevel(stationUid.Value, component.AlertLevelOnDeactivate, true, true, true);

            // warn a crew
            var announcement = Loc.GetString("nuke-component-announcement-unarmed");
            var sender = Loc.GetString("nuke-component-announcement-sender");
            _chatSystem.DispatchStationAnnouncement(uid, announcement, sender, false);

            component.PlayedNukeSong = false;
            NukeDisarmedAudio(component);

            // disable sound and reset it
            component.PlayedAlertSound = false;
            component.AlertAudioStream?.Stop();

            // start bomb cooldown
            _itemSlots.SetLock(uid, component.DiskSlot, false);
            component.Status = NukeStatus.COOLDOWN;
            component.CooldownTime = component.Cooldown;

            UpdateUserInterface(uid, component);
        }

        /// <summary>
        ///     Toggle bomb arm button
        /// </summary>
        public void ToggleBomb(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Status == NukeStatus.ARMED)
                DisarmBomb(uid, component);
            else
                ArmBomb(uid, component);
        }

        /// <summary>
        ///     Force bomb to explode immediately
        /// </summary>
        public void ActivateBomb(EntityUid uid, NukeComponent? component = null,
            TransformComponent? transform = null)
        {
            if (!Resolve(uid, ref component, ref transform))
                return;

            if (component.Exploded)
                return;

            component.Exploded = true;

            _explosions.QueueExplosion(uid,
                component.ExplosionType,
                component.TotalIntensity,
                component.IntensitySlope,
                component.MaxIntensity);

            RaiseLocalEvent(new NukeExplodedEvent()
            {
                OwningStation = transform.GridUid,
            });

            _soundSystem.StopStationEventMusic(component.Owner, StationEventMusicType.Nuke);
            EntityManager.DeleteEntity(uid);
        }

        /// <summary>
        ///     Set remaining time value
        /// </summary>
        public void SetRemainingTime(EntityUid uid, float timer, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.RemainingTime = timer;
            UpdateUserInterface(uid, component);
        }

        #endregion

        private void DisarmBombDoafter(EntityUid uid, EntityUid user, NukeComponent nuke)
        {
            nuke.DisarmCancelToken = new();
            var doafter = new DoAfterEventArgs(user, nuke.DisarmDoafterLength, nuke.DisarmCancelToken.Value, uid)
            {
                TargetCancelledEvent = new NukeDisarmCancelledEvent(),
                TargetFinishedEvent = new NukeDisarmSuccessEvent(),
                BroadcastFinishedEvent = new NukeDisarmSuccessEvent(),
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true,
            };

            _doAfterSystem.DoAfter(doafter);
            _popups.PopupEntity(Loc.GetString("nuke-component-doafter-warning"), user,
                user, PopupType.LargeCaution);
        }

        private void NukeArmedAudio(NukeComponent component)
        {
            _soundSystem.PlayGlobalOnStation(component.Owner, component.ArmSound.GetSound());
        }

        private void NukeDisarmedAudio(NukeComponent component)
        {
            _soundSystem.PlayGlobalOnStation(component.Owner, component.DisarmSound.GetSound());
            _soundSystem.StopStationEventMusic(component.Owner, StationEventMusicType.Nuke);
        }
    }

    public sealed class NukeExplodedEvent : EntityEventArgs
    {
        public EntityUid? OwningStation;
    }

    /// <summary>
    ///     Raised directed on the nuke when its disarm doafter is successful.
    /// </summary>
    public sealed class NukeDisarmSuccessEvent : EntityEventArgs
    {

    }

    /// <summary>
    ///     Raised directed on the nuke when its disarm doafter is cancelled.
    /// </summary>
    public sealed class NukeDisarmCancelledEvent : EntityEventArgs
    {

    }

}
