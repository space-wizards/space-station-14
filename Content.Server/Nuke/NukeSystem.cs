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

        public const string DiskSlotName = "DiskSlot";
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<NukeComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<NukeComponent, ItemSlotChangedEvent>(OnItemSlotChanged);
        }

        private void OnItemSlotChanged(EntityUid uid, NukeComponent component, ItemSlotChangedEvent args)
        {
            if (args.SlotName != DiskSlotName)
                return;

            component.DiskInserted = args.ContainedItem != null;
            UpdateUserInterface(uid, component);
        }

        private void OnActivate(EntityUid uid, NukeComponent component, ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            // standard interactions check
            if (!args.InRangeUnobstructed())
                return;
            if (!_actionBlocker.CanInteract(uid) || !_actionBlocker.CanUse(uid))
                return;

            if (!EntityManager.TryGetComponent(args.User.Uid, out ActorComponent? actor))
                return;

            ToggleUI(uid, actor.PlayerSession, component);
            args.Handled = true;
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

            var state = new NukeUiState()
            {
                IsArmed = component.IsArmed,
                NukeDiskInserted = component.DiskInserted,
                RemainingTime = component.RemainingTime
            };

            ui.SetState(state);
        }
    }
}
