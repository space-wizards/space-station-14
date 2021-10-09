using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.Access;
using Content.Shared.Acts;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Access.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class IdCardConsoleComponent : SharedIdCardConsoleComponent, IActivate, IInteractUsing, IBreakAct
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public ContainerSlot PrivilegedIdContainer = default!;
        public ContainerSlot TargetIdContainer = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(IdCardConsoleUiKey.Key);
        [ViewVariables] private bool Powered => !Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || receiver.Powered;

        public bool PrivilegedIDEmpty => PrivilegedIdContainer.ContainedEntities.Count < 1;
        public bool TargetIDEmpty => TargetIdContainer.ContainedEntities.Count < 1;

        protected override void Initialize()
        {
            base.Initialize();

            PrivilegedIdContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-privilegedId");
            TargetIdContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-targetId");

            Owner.EnsureComponentWarn<AccessReader>();
            Owner.EnsureComponentWarn<ServerUserInterfaceComponent>();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUiReceiveMessage;
            }

            UpdateUserInterface();
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
                            HandleId(obj.Session.AttachedEntity, PrivilegedIdContainer);
                            break;
                        case UiButton.TargetId:
                            HandleId(obj.Session.AttachedEntity, TargetIdContainer);
                            break;
                    }
                    break;
                case WriteToTargetIdMessage msg:
                    TryWriteToTargetId(msg.FullName, msg.JobTitle, msg.AccessList);
                    break;
            }

            UpdateUserInterface();
        }

        /// <summary>
        /// Returns true if there is an ID in <see cref="PrivilegedIdContainer"/> and said ID satisfies the requirements of <see cref="AccessReader"/>.
        /// </summary>
        private bool PrivilegedIdIsAuthorized()
        {
            if (!Owner.TryGetComponent(out AccessReader? reader))
            {
                return true;
            }

            var privilegedIdEntity = PrivilegedIdContainer.ContainedEntity;
            return privilegedIdEntity != null && reader.IsAllowed(privilegedIdEntity);
        }

        /// <summary>
        /// Called when the "Submit" button in the UI gets pressed.
        /// Writes data passed from the UI into the ID stored in <see cref="TargetIdContainer"/>, if present.
        /// </summary>
        private void TryWriteToTargetId(string newFullName, string newJobTitle, List<string> newAccessList)
        {
            if (!PrivilegedIdIsAuthorized() || TargetIdContainer.ContainedEntity == null)
            {
                return;
            }

            var targetIdEntity = TargetIdContainer.ContainedEntity;

            var targetIdComponent = targetIdEntity.GetComponent<IdCardComponent>();
            targetIdComponent.FullName = newFullName;
            targetIdComponent.JobTitle = newJobTitle;

            if (!newAccessList.TrueForAll(x => _prototypeManager.HasIndex<AccessLevelPrototype>(x)))
            {
                Logger.Warning("Tried to write unknown access tag.");
                return;
            }
            var targetIdAccess = targetIdEntity.GetComponent<AccessComponent>();
            targetIdAccess.SetTags(newAccessList);
        }

        /// <summary>
        /// Called when one of the insert/remove ID buttons gets pressed.
        /// </summary>
        private void HandleId(IEntity user, ContainerSlot container)
        {
            if (!user.TryGetComponent(out SharedHandsComponent? hands))
            {
                Owner.PopupMessage(user, Loc.GetString("access-id-card-console-component-no-hands-error"));
                return;
            }

            if (container.ContainedEntity == null)
            {
                InsertIdFromHand(user, container, hands);
            }
            else
            {
                PutIdInHand(container, hands);
            }
        }

        public void InsertIdFromHand(IEntity user, ContainerSlot container, SharedHandsComponent hands)
        {
            if (!hands.TryGetActiveHeldEntity(out var heldEntity))
                return;

            if (!heldEntity.HasComponent<IdCardComponent>())
                return;

            if (!hands.TryPutHandIntoContainer(hands.ActiveHand!, container))
            {
                Owner.PopupMessage(user, Loc.GetString("access-id-card-console-component-cannot-let-go-error"));
                return;
            }
            UpdateUserInterface();
        }

        public void PutIdInHand(ContainerSlot container, SharedHandsComponent hands)
        {
            var idEntity = container.ContainedEntity;
            if (idEntity == null || !container.Remove(idEntity))
            {
                return;
            }
            UpdateUserInterface();

            hands.TryPutInActiveHandOrAny(idEntity);
        }

        private void UpdateUserInterface()
        {
            var isPrivilegedIdPresent = PrivilegedIdContainer.ContainedEntity != null;
            var targetIdEntity = TargetIdContainer.ContainedEntity;
            IdCardConsoleBoundUserInterfaceState newState;
            // this could be prettier
            if (targetIdEntity == null)
            {
                newState = new IdCardConsoleBoundUserInterfaceState(
                    isPrivilegedIdPresent,
                    PrivilegedIdIsAuthorized(),
                    false,
                    null,
                    null,
                    null,
                    PrivilegedIdContainer.ContainedEntity?.Name ?? string.Empty,
                    TargetIdContainer.ContainedEntity?.Name ?? string.Empty);
            }
            else
            {
                var targetIdComponent = targetIdEntity.GetComponent<IdCardComponent>();
                var targetAccessComponent = targetIdEntity.GetComponent<AccessComponent>();
                newState = new IdCardConsoleBoundUserInterfaceState(
                    isPrivilegedIdPresent,
                    PrivilegedIdIsAuthorized(),
                    true,
                    targetIdComponent.FullName,
                    targetIdComponent.JobTitle,
                    targetAccessComponent.Tags.ToArray(),
                    PrivilegedIdContainer.ContainedEntity?.Name ?? string.Empty,
                    TargetIdContainer.ContainedEntity?.Name ?? string.Empty);
            }
            UserInterface?.SetState(newState);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }
            if (!Powered) return;

            UserInterface?.Open(actor.PlayerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            var user = eventArgs.User;

            if (!PrivilegedIDEmpty && !TargetIDEmpty)
            {
                return false;
            }

            if (!item.HasComponent<IdCardComponent>() || !user.TryGetComponent(out SharedHandsComponent? hand))
            {
                return false;
            }

            if (PrivilegedIDEmpty)
            {
                InsertIdFromHand(user, PrivilegedIdContainer, hand);
            }

            else if (TargetIDEmpty)
            {
                InsertIdFromHand(user, TargetIdContainer, hand);
            }

            UpdateUserInterface();
            return true;
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            var privileged = PrivilegedIdContainer.ContainedEntity;
            if (privileged != null)
                PrivilegedIdContainer.Remove(privileged);

            var target = TargetIdContainer.ContainedEntity;
            if (target != null)
                TargetIdContainer.Remove(target);
        }
    }
}
