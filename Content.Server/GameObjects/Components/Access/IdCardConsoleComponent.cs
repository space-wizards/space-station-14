using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Access;
using Content.Shared.GameObjects.Components.Access;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class IdCardConsoleComponent : SharedIdCardConsoleComponent, IActivate
    {
#pragma warning disable 649
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
#pragma warning restore 649

        private BoundUserInterface _userInterface;
        private ContainerSlot _privilegedIdContainer;
        private ContainerSlot _targetIdContainer;
        private AccessReader _accessReader;

        public override void Initialize()
        {
            base.Initialize();

            _privilegedIdContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-privilegedId", Owner);
            _targetIdContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-targetId", Owner);

            _accessReader = Owner.GetComponent<AccessReader>();

            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(IdCardConsoleUiKey.Key);
            _userInterface.OnReceiveMessage += OnUiReceiveMessage;
            UpdateUserInterface();
        }

        private void OnUiReceiveMessage(ServerBoundUserInterfaceMessage obj)
        {
            switch (obj.Message)
            {
                case IdButtonPressedMessage msg:
                    switch (msg.Button)
                    {
                        case UiButton.PrivilegedId:
                            HandleId(obj.Session.AttachedEntity, _privilegedIdContainer);
                            break;
                        case UiButton.TargetId:
                            HandleId(obj.Session.AttachedEntity, _targetIdContainer);
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
        /// Returns true if there is an ID in <see cref="_privilegedIdContainer"/> and said ID satisfies the requirements of <see cref="_accessReader"/>.
        /// </summary>
        private bool PrivilegedIdIsAuthorized()
        {
            var privilegedIdEntity = _privilegedIdContainer.ContainedEntity;
            return privilegedIdEntity != null && _accessReader.IsAllowed(privilegedIdEntity);
        }
        /// <summary>
        /// Called when the "Submit" button in the UI gets pressed.
        /// Writes data passed from the UI into the ID stored in <see cref="_targetIdContainer"/>, if present.
        /// </summary>
        private void TryWriteToTargetId(string newFullName, string newJobTitle, List<string> newAccessList)
        {
            if (!PrivilegedIdIsAuthorized() || _targetIdContainer.ContainedEntity == null)
            {
                return;
            }

            var targetIdEntity = _targetIdContainer.ContainedEntity;

            var targetIdComponent = targetIdEntity.GetComponent<IdCardComponent>();
            targetIdComponent.FullName = newFullName;
            targetIdComponent.JobTitle = newJobTitle;

            if (!newAccessList.TrueForAll(x => _prototypeManager.HasIndex<AccessLevelPrototype>(x)))
            {
                Logger.Warning($"Tried to write unknown access tag.");
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
            if (!user.TryGetComponent(out IHandsComponent hands))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user, _localizationManager.GetString("You have no hands."));
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

        private void InsertIdFromHand(IEntity user, ContainerSlot container, IHandsComponent hands)
        {
            var isId = hands.GetActiveHand?.Owner.HasComponent<IdCardComponent>();
            if (isId != true)
            {
                return;
            }
            if(!hands.Drop(hands.ActiveHand, container))
            {
                _notifyManager.PopupMessage(Owner.Transform.GridPosition, user, _localizationManager.GetString("You can't let go of the ID card!"));
                return;
            }
            UpdateUserInterface();
        }

        private void PutIdInHand(ContainerSlot container, IHandsComponent hands)
        {
            var idEntity = container.ContainedEntity;
            if (idEntity == null || !container.Remove(idEntity))
            {
                return;
            }
            UpdateUserInterface();

            hands.PutInHand(idEntity.GetComponent<ItemComponent>());
        }

        private void UpdateUserInterface()
        {
            var isPrivilegedIdPresent = _privilegedIdContainer.ContainedEntity != null;
            var targetIdEntity = _targetIdContainer.ContainedEntity;
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
                    _privilegedIdContainer.ContainedEntity?.Name ?? "",
                    _targetIdContainer.ContainedEntity?.Name ?? "");
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
                    _privilegedIdContainer.ContainedEntity?.Name ?? "",
                    _targetIdContainer.ContainedEntity?.Name ?? "");
            }
            _userInterface.SetState(newState);
        }

        public void Activate(ActivateEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }

            _userInterface.Open(actor.playerSession);
        }
    }
}
