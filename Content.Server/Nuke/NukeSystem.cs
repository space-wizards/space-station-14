using System.Collections.Generic;
using Content.Server.Construction.Components;
using Content.Server.Popups;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Body.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Nuke;
using Robust.Server.GameObjects;
using Robust.Server.Player;
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
        [Dependency] private readonly SharedItemSlotsSystem _itemSlots = default!;
        [Dependency] private readonly PopupSystem _popups = default!;
        [Dependency] private readonly IEntityLookup _lookup = default!;

        private readonly HashSet<EntityUid> _tickingBombs = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<NukeComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<NukeComponent, ItemSlotChangedEvent>(OnItemSlotChanged);

            // anchoring logic
            SubscribeLocalEvent<NukeComponent, AnchorAttemptEvent>(OnAnchorAttempt);
            SubscribeLocalEvent<NukeComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<NukeComponent, AnchoredEvent>(OnWasAnchored);
            SubscribeLocalEvent<NukeComponent, UnanchoredEvent>(OnWasUnanchored);

            // ui events
            SubscribeLocalEvent<NukeComponent, NukeEjectMessage>(OnEject);
            SubscribeLocalEvent<NukeComponent, NukeAnchorMessage>(OnAnchor);
            SubscribeLocalEvent<NukeComponent, NukeArmedMessage>(OnArmed);
            SubscribeLocalEvent<NukeComponent, NukeKeypadMessage>(OnKeypad);
            SubscribeLocalEvent<NukeComponent, NukeKeypadClearMessage>(OnClear);
            SubscribeLocalEvent<NukeComponent, NukeKeypadEnterMessage>(OnEnter);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var uid in _tickingBombs)
            {
                if (!EntityManager.TryGetComponent(uid, out NukeComponent nuke))
                    continue;

                nuke.RemainingTime -= frameTime;
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
        }

        private void OnArmed(EntityUid uid, NukeComponent component, NukeArmedMessage args)
        {
            if (component.Status == NukeStatus.AWAIT_ARM)
            {
                ArmBomb(uid, component);
            }
            else
            {
                DisarmBomb(uid, component);
            }
        }

        private void OnEnter(EntityUid uid, NukeComponent component, NukeKeypadEnterMessage args)
        {
            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            UpdateStatus(uid, component);
            UpdateUserInterface(uid, component);
        }

        private void OnKeypad(EntityUid uid, NukeComponent component, NukeKeypadMessage args)
        {
            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            if (component.EnteredCode.Length >= _codes.Code.Length)
                return;

            component.EnteredCode += args.Value.ToString();
            UpdateUserInterface(uid, component);
        }

        private void OnClear(EntityUid uid, NukeComponent component, NukeKeypadClearMessage args)
        {
            if (component.Status != NukeStatus.AWAIT_CODE)
                return;

            component.EnteredCode = "";
            UpdateUserInterface(uid, component);
        }

        private void OnItemSlotChanged(EntityUid uid, NukeComponent component, ItemSlotChangedEvent args)
        {
            if (args.SlotName != component.DiskSlotName)
                return;

            component.DiskInserted = args.ContainedItem != null;
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

            if (!EntityManager.TryGetComponent(args.User.Uid, out ActorComponent? actor))
                return;

            ToggleUI(uid, actor.PlayerSession, component);
            args.Handled = true;
        }

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
            if (!component.DiskInserted)
            {
                var msg = Loc.GetString("nuke-component-cant-anchor");
                _popups.PopupEntity(msg, uid, Filter.Entities(args.User.Uid));

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

        private void OnEject(EntityUid uid, NukeComponent component, NukeEjectMessage args)
        {
            _itemSlots.TryEjectContent(uid, component.DiskSlotName, args.Session.AttachedEntity);
        }

        private async void OnAnchor(EntityUid uid, NukeComponent component, NukeAnchorMessage args)
        {
            if (!EntityManager.TryGetComponent(uid, out AnchorableComponent anchorable))
                return;

            var user = args.Session.AttachedEntity;
            if (user == null)
                return;

            await anchorable.TryToggleAnchor(user, null);
        }

        private void UpdateStatus(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            switch (component.Status)
            {
                case NukeStatus.AWAIT_DISK:
                    if (component.DiskInserted)
                        component.Status = NukeStatus.AWAIT_CODE;
                    break;
                case NukeStatus.AWAIT_CODE:
                {
                    if (!component.DiskInserted)
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
                    }
                    else
                    {
                        component.EnteredCode = "";
                    }
                    break;
                }
            }

        }

        private void ToggleUI(EntityUid uid, IPlayerSession session, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(NukeUiKey.Key);
            ui?.Toggle(session);

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
            if (EntityManager.TryGetComponent(uid, out ITransformComponent transform))
                anchored = transform.Anchored;

            var allowArm = component.DiskInserted &&
                           (component.Status == NukeStatus.AWAIT_ARM ||
                            component.Status == NukeStatus.TIMING);

            var state = new NukeUiState()
            {
                Status = component.Status,
                RemainingTime = (int) component.RemainingTime,
                DiskInserted = component.DiskInserted,
                IsAnchored = anchored,
                AllowArm = allowArm,
                EnteredCodeLength = component.EnteredCode.Length,
                MaxCodeLength = _codes.Code.Length
            };

            ui.SetState(state);
        }

        public void ArmBomb(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Status = NukeStatus.TIMING;
            _tickingBombs.Add(uid);
            UpdateUserInterface(uid, component);
        }

        public void DisarmBomb(EntityUid uid, NukeComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            component.Status = NukeStatus.AWAIT_ARM;
            _tickingBombs.Remove(uid);
            UpdateUserInterface(uid, component);
        }

        public void ActivateBomb(EntityUid uid, NukeComponent? component = null,
            ITransformComponent? transform = null)
        {
            if (!Resolve(uid, ref component, ref transform))
                return;

            // gib anyone in a blast radius
            // its lame, but will work for now
            var pos = transform.Coordinates;
            var ents = _lookup.GetEntitiesInRange(pos, component.BlastRadius);
            foreach (var ent in ents)
            {
                var entUid = ent.Uid;
                if (!EntityManager.EntityExists(entUid))
                    continue;;

                if (EntityManager.TryGetComponent(entUid, out SharedBodyComponent? body))
                    body.Gib();
            }

            EntityManager.DeleteEntity(uid);

        }
    }
}
