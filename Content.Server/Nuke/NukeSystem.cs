using Content.Server.Chat.Managers;
using Content.Server.Construction.Components;
using Content.Server.Coordinates.Helpers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Nuke;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Nuke
{
    public sealed class NukeSystem : EntitySystem
    {
        [Dependency] private readonly NukeCodeSystem _codes = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly ExplosionSystem _explosions = default!;
        [Dependency] private readonly IChatManager _chat = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<NukeComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<NukeComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
            SubscribeLocalEvent<NukeComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);

            // anchoring logic
            SubscribeLocalEvent<NukeComponent, AnchorAttemptEvent>(OnAnchorAttempt);
            SubscribeLocalEvent<NukeComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<NukeComponent, AnchorStateChangedEvent>(OnAnchorChanged);

            // ui events
            SubscribeLocalEvent<NukeComponent, NukeEjectMessage>(OnEjectButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeAnchorMessage>(OnAnchorButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeArmedMessage>(OnArmButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeKeypadMessage>(OnKeypadButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeKeypadClearMessage>(OnClearButtonPressed);
            SubscribeLocalEvent<NukeComponent, NukeKeypadEnterMessage>(OnEnterButtonPressed);
        }

        private void OnInit(EntityUid uid, NukeComponent component, ComponentInit args)
        {
            component.RemainingTime = component.Timer;
            _itemSlots.AddItemSlot(uid, component.Name, component.DiskSlot);

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

        private void OnRemove(EntityUid uid, NukeComponent component, ComponentRemove args)
        {
            _itemSlots.RemoveItemSlot(uid, component.DiskSlot);
        }

        private void OnItemSlotChanged(EntityUid uid, NukeComponent component, ContainerModifiedMessage args)
        {
            if (!component.Initialized) return;

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
            // cancel any anchor attempt without nuke disk
            if (!component.DiskSlot.HasItem)
            {
                var msg = Loc.GetString("nuke-component-cant-anchor");
                _popups.PopupEntity(msg, uid, Filter.Entities(args.User));

                args.Cancel();
            }
        }

        private void OnAnchorChanged(EntityUid uid, NukeComponent component, ref AnchorStateChangedEvent args)
        {
            UpdateUserInterface(uid, component);
        }
        #endregion

        #region UI Events
        private void OnEjectButtonPressed(EntityUid uid, NukeComponent component, NukeEjectMessage args)
        {
            if (!component.DiskSlot.HasItem)
                return;

            _itemSlots.TryEjectToHands(uid, component.DiskSlot, args.Session.AttachedEntity);
        }

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
            PlaySound(uid, component.KeypadPressSound, 0.125f, component);

            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            if (component.EnteredCode.Length >= _codes.Code.Length)
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

            if (component.Status == NukeStatus.AWAIT_ARM)
            {
                ArmBomb(uid, component);
            }
            else
            {
                DisarmBomb(uid, component);
            }
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

            // play alert sound if time is running out
            if (nuke.RemainingTime <= nuke.AlertSoundTime && !nuke.PlayedAlertSound)
            {
                nuke.AlertAudioStream = SoundSystem.Play(Filter.Broadcast(), nuke.AlertSound.GetSound());
                nuke.PlayedAlertSound = true;
            }

            if (nuke.RemainingTime <= 0)
            {
                nuke.RemainingTime = 0;
                ActivateBomb(uid, nuke);
            }
            else
            {
                UpdateUserInterface(uid, nuke);
            }
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

                        var isValid = _codes.IsCodeValid(component.EnteredCode);
                        if (isValid)
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
            if (EntityManager.TryGetComponent(uid, out TransformComponent transform))
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
                MaxCodeLength = _codes.Code.Length,
                CooldownTime = (int) component.CooldownTime
            };

            ui.SetState(state);
        }

        private void PlaySound(EntityUid uid, SoundSpecifier sound, float varyPitch = 0f,
            NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            SoundSystem.Play(Filter.Pvs(uid), sound.GetSound(),
                uid, AudioHelpers.WithVariation(varyPitch).WithVolume(-5f));
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

            // warn a crew
            var announcement = Loc.GetString("nuke-component-announcement-armed",
                ("time", (int) component.RemainingTime));
            var sender = Loc.GetString("nuke-component-announcement-sender");
            _chat.DispatchStationAnnouncement(announcement, sender, false);

            // todo: move it to announcements system
            SoundSystem.Play(Filter.Broadcast(), component.ArmSound.GetSound());

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

            // warn a crew
            var announcement = Loc.GetString("nuke-component-announcement-unarmed");
            var sender = Loc.GetString("nuke-component-announcement-sender");
            _chat.DispatchStationAnnouncement(announcement, sender, false);

            // todo: move it to announcements system
            SoundSystem.Play(Filter.Broadcast(), component.DisarmSound.GetSound());

            // disable sound and reset it
            component.PlayedAlertSound = false;
            component.AlertAudioStream?.Stop();

            // start bomb cooldown
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
    }
}
