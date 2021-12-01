using System.Collections.Generic;
using System.Linq;
using Content.Server.Access.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Access;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIdCardConsoleComponent))]
    public sealed class IdCardConsoleComponent : SharedIdCardConsoleComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(IdCardConsoleUiKey.Key);
        [ViewVariables] private bool Powered => !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<AccessReader>();
            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            if (obj.Session.AttachedEntity == null)
            {
                return;
            }

            switch (obj.Message)
            {
                case IdButtonPressedMessage msg:
                    switch (msg.Button)
                    {
                        case UiButton.PrivilegedId:
                            HandleIdButton(obj.Session.AttachedEntity, PrivilegedIdSlot);
                            break;
                        case UiButton.TargetId:
                            HandleIdButton(obj.Session.AttachedEntity, TargetIdSlot);
                            break;
                    }
                    break;
                case WriteToTargetIdMessage msg:
                    TryWriteToTargetId(msg.FullName, msg.JobTitle, msg.AccessList);
                    UpdateUserInterface();
                    break;
            }
        }

        /// <summary>
        /// Returns true if there is an ID in <see cref="PrivilegedIdSlot"/> and said ID satisfies the requirements of <see cref="AccessReader"/>.
        /// </summary>
        private bool PrivilegedIdIsAuthorized()
        {
            if (!Owner.TryGetComponent(out AccessReader? reader))
            {
                return true;
            }

            var privilegedIdEntity = PrivilegedIdSlot.Item;
            var accessSystem = EntitySystem.Get<AccessReaderSystem>();
            return privilegedIdEntity != null && accessSystem.IsAllowed(reader, privilegedIdEntity.Uid);
        }

        /// <summary>
        /// Called when the "Submit" button in the UI gets pressed.
        /// Writes data passed from the UI into the ID stored in <see cref="TargetIdSlot"/>, if present.
        /// </summary>
        private void TryWriteToTargetId(string newFullName, string newJobTitle, List<string> newAccessList)
        {
            var targetIdEntity = TargetIdSlot.Item;
            if (targetIdEntity == null || !PrivilegedIdIsAuthorized())
                return;

            var cardSystem = EntitySystem.Get<IdCardSystem>();
            cardSystem.TryChangeFullName(targetIdEntity.Uid, newFullName);
            cardSystem.TryChangeJobTitle(targetIdEntity.Uid, newJobTitle);

            if (!newAccessList.TrueForAll(x => _prototypeManager.HasIndex<AccessLevelPrototype>(x)))
            {
                Logger.Warning("Tried to write unknown access tag.");
                return;
            }

            var accessSystem = EntitySystem.Get<AccessSystem>();
            accessSystem.TrySetTags(targetIdEntity.Uid, newAccessList);
        }

        /// <summary>
        /// Called when one of the insert/remove ID buttons gets pressed.
        /// </summary>
        private void HandleIdButton(IEntity user, ItemSlot slot)
        {
            if (slot.HasItem)
                EntitySystem.Get<ItemSlotsSystem>().TryEjectToHands(OwnerUid, slot, user.Uid);
            else
                EntitySystem.Get<ItemSlotsSystem>().TryInsertFromHand(OwnerUid, slot, user.Uid);
        }

        public void UpdateUserInterface()
        {
            if (!Initialized)
                return;

            var targetIdEntity = TargetIdSlot.Item;
            IdCardConsoleBoundUserInterfaceState newState;
            // this could be prettier
            if (targetIdEntity == null)
            {
                newState = new IdCardConsoleBoundUserInterfaceState(
                    PrivilegedIdSlot.HasItem,
                    PrivilegedIdIsAuthorized(),
                    false,
                    null,
                    null,
                    null,
                    PrivilegedIdSlot.Item?.Name ?? string.Empty,
                    string.Empty);
            }
            else
            {
                var targetIdComponent = targetIdEntity.GetComponent<IdCardComponent>();
                var targetAccessComponent = targetIdEntity.GetComponent<AccessComponent>();
                newState = new IdCardConsoleBoundUserInterfaceState(
                    PrivilegedIdSlot.HasItem,
                    PrivilegedIdIsAuthorized(),
                    true,
                    targetIdComponent.FullName,
                    targetIdComponent.JobTitle,
                    targetAccessComponent.Tags.ToArray(),
                    PrivilegedIdSlot.Item?.Name ?? string.Empty,
                    targetIdEntity.Name);
            }
            UserInterface?.SetState(newState);
        }
    }
}
