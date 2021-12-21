using System.Collections.Generic;
using Content.Server.Chat.Managers;
using Content.Server.Construction.Components;
using Content.Server.Coordinates.Helpers;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Body.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Nuke;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Nuke
{
    public class NukeSystem : EntitySystem
    {
        [Dependency] private readonly NukeCodeSystem _codes = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;
        [Dependency] private readonly IChatManager _chat = default!;

        private readonly HashSet<EntityUid> _tickingBombs = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<NukeComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<NukeComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<NukeComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
            SubscribeLocalEvent<NukeComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);

            // anchoring logic
            SubscribeLocalEvent<NukeComponent, AnchorAttemptEvent>(OnAnchorAttempt);
            SubscribeLocalEvent<NukeComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<NukeComponent, AnchoredEvent>(OnWasAnchored);
            SubscribeLocalEvent<NukeComponent, UnanchoredEvent>(OnWasUnanchored);

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
            foreach (var uid in _tickingBombs)
            {
                if (!EntityManager.TryGetComponent(uid, out NukeComponent nuke))
                    continue;

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
        }

        private void OnRemove(EntityUid uid, NukeComponent component, ComponentRemove args)
        {
            _tickingBombs.Remove(uid);
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

        private void OnActivate(EntityUid uid, NukeComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            // standard interactions check
            if (!args.InRangeUnobstructed())
                return;
            if (!_actionBlocker.CanInteract(args.User) || !_actionBlocker.CanUse(args.User))
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            ShowUI(uid, actor.PlayerSession, component);
            args.Handled = true;
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

        private void OnWasUnanchored(EntityUid uid, NukeComponent component, UnanchoredEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnWasAnchored(EntityUid uid, NukeComponent component, AnchoredEvent args)
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
            PlaydSound(uid, component.KeypadPressSound, 0.125f, component);

            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            if (component.EnteredCode.Length >= _codes.Code.Length)
                return;

            component.EnteredCode += args.Value.ToString();
            UpdateUserInterface(uid, component);
        }

        private void OnClearButtonPressed(EntityUid uid, NukeComponent component, NukeKeypadClearMessage args)
        {
            PlaydSound(uid, component.KeypadPressSound, 0f, component);

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
                            PlaydSound(uid, component.AccessGrantedSound, 0, component);
                        }
                        else
                        {
                            component.EnteredCode = "";
                            PlaydSound(uid, component.AccessDeniedSound, 0, component);
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

        private void ShowUI(EntityUid uid, IPlayerSession session, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(NukeUiKey.Key);
            ui?.Open(session);

            UpdateUserInterface(uid, component);
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
                MaxCodeLength = _codes.Code.Length
            };

            ui.SetState(state);
        }

        private void PlaydSound(EntityUid uid, SoundSpecifier sound, float varyPitch = 0f,
            NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            SoundSystem.Play(Filter.Pvs(uid), sound.GetSound(),
                uid, AudioHelpers.WithVariation(varyPitch));
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
            _tickingBombs.Add(uid);
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

            component.Status = NukeStatus.AWAIT_ARM;
            _tickingBombs.Remove(uid);
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

            // gib anyone in a blast radius
            // its lame, but will work for now
            var pos = transform.Coordinates;
            var ents = _lookup.GetEntitiesInRange(pos, component.BlastRadius);
            foreach (var ent in ents)
            {
                var entUid = ent;
                if (!EntityManager.EntityExists(entUid))
                    continue;;

                if (EntityManager.TryGetComponent(entUid, out SharedBodyComponent? body))
                    body.Gib();
            }

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
