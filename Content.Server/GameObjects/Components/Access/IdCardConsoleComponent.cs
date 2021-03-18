#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Access;
using Content.Shared.GameObjects.Components.Access;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Access
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class IdCardConsoleComponent : SharedIdCardConsoleComponent, IActivate, IInteractUsing, IBreakAct
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private ContainerSlot _privilegedIdContainer = default!;
        private ContainerSlot _targetIdContainer = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(IdCardConsoleUiKey.Key);

        private bool PrivilegedIDEmpty => _privilegedIdContainer.ContainedEntities.Count < 1;
        private bool TargetIDEmpty => _targetIdContainer.ContainedEntities.Count < 1;

        public override void Initialize()
        {
            base.Initialize();

            _privilegedIdContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-privilegedId");
            _targetIdContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-targetId");

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
        /// Returns true if there is an ID in <see cref="_privilegedIdContainer"/> and said ID satisfies the requirements of <see cref="AccessReader"/>.
        /// </summary>
        private bool PrivilegedIdIsAuthorized()
        {
            if (!Owner.TryGetComponent(out AccessReader? reader))
            {
                return true;
            }

            var privilegedIdEntity = _privilegedIdContainer.ContainedEntity;
            return privilegedIdEntity != null && reader.IsAllowed(privilegedIdEntity);
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
            if (!user.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(user, Loc.GetString("You have no hands."));
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

            if (hands.ActiveHand == null)
            {
                return;
            }

            if (!hands.TryPutHandIntoContainer(hands.ActiveHand, container))
            {
                Owner.PopupMessage(user, Loc.GetString("You can't let go of the ID card!"));
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
            UserInterface?.SetState(newState);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if(!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            UserInterface?.Open(actor.playerSession);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            var user = eventArgs.User;

            if (!PrivilegedIDEmpty && !TargetIDEmpty)
            {
                return false;
            }

            if (!item.TryGetComponent<IdCardComponent>(out var idCardComponent) || !user.TryGetComponent(out IHandsComponent? hand))
            {
                return false;
            }

            if (PrivilegedIDEmpty)
            {
                InsertIdFromHand(user, _privilegedIdContainer, hand);
            }

            else if (TargetIDEmpty)
            {
                InsertIdFromHand(user, _targetIdContainer, hand);
            }

            UpdateUserInterface();
            return true;
        }

        [Verb]
        public sealed class EjectPrivilegedIDVerb : Verb<IdCardConsoleComponent>
        {
            protected override void GetData(IEntity user, IdCardConsoleComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject Privileged ID");
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
                data.Visibility = component.PrivilegedIDEmpty ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, IdCardConsoleComponent component)
            {
                if (!user.TryGetComponent(out IHandsComponent? hand))
                {
                    return;
                }
                component.PutIdInHand(component._privilegedIdContainer, hand);
            }
        }

        public sealed class EjectTargetIDVerb : Verb<IdCardConsoleComponent>
        {
            protected override void GetData(IEntity user, IdCardConsoleComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject Target ID");
                data.Visibility = component.TargetIDEmpty ? VerbVisibility.Invisible : VerbVisibility.Visible;
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, IdCardConsoleComponent component)
            {
                if (!user.TryGetComponent(out IHandsComponent? hand))
                {
                    return;
                }
                component.PutIdInHand(component._targetIdContainer, hand);
            }
        }

        public void OnBreak(BreakageEventArgs eventArgs)
        {
            var privileged = _privilegedIdContainer.ContainedEntity;
            if (privileged != null)
                _privilegedIdContainer.Remove(privileged);

            var target = _targetIdContainer.ContainedEntity;
            if (target != null)
                _targetIdContainer.Remove(target);
        }
    }
}
