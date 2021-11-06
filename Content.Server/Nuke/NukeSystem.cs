using Content.Server.Construction.Components;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Nuke;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Nuke
{
    public class NukeSystem : EntitySystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly SharedItemSlotsSystem _itemSlots = default!;

        public const string DiskSlotName = "DiskSlot";
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<NukeComponent, ItemSlotChangedEvent>(OnItemSlotChanged);
            SubscribeLocalEvent<NukeComponent, AnchoredEvent>(OnWasAnchored);
            SubscribeLocalEvent<NukeComponent, UnanchoredEvent>(OnWasUnanchored);

            // ui events
            SubscribeLocalEvent<NukeComponent, NukeEjectMessage>(OnEject);
            SubscribeLocalEvent<NukeComponent, NukeAnchorMessage>(OnAnchor);
        }

        private void OnItemSlotChanged(EntityUid uid, NukeComponent component, ItemSlotChangedEvent args)
        {
            if (args.SlotName != DiskSlotName)
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
            _itemSlots.TryEjectContent(uid, DiskSlotName, args.Session.AttachedEntity);
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
                    if (!component.DiskInserted)
                        component.Status = NukeStatus.AWAIT_DISK;
                    break;
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

            var state = new NukeUiState()
            {
                Status = component.Status,
                RemainingTime = component.RemainingTime,
                DiskInserted = component.DiskInserted,
                IsAnchored = anchored
            };

            ui.SetState(state);
        }
    }
}
