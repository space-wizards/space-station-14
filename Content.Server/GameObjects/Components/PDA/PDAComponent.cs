using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.PDA;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Localization;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.PDA
{
    [RegisterComponent]
    public class PDAComponent : SharedPDAComponent, IAttackBy, IUse
    {
        private Container _idSlot;
        private PointLightComponent _pdaLight;
        private bool _lightOn = false;
        private BoundUserInterface _interface;
        public bool IdSlotEmpty => _idSlot.ContainedEntities.Count < 1;
        public IEntity OriginalOwner { get; private set; }

        public IdCardComponent ContainedID { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            _idSlot = ContainerManagerComponent.Ensure<Container>("pda_entity_container", Owner, out var existed);
            _pdaLight = Owner.GetComponent<PointLightComponent>();
            _interface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(PDAUiKey.Key);
            _interface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case PDAToggleFlashlightMessage msg:
                {
                    ToggleLight();
                    break;
                }

                case PDAEjectIDMessage msg:
                {
                    HandleIDEjection(message.Session.AttachedEntity);
                    break;
                }
            }
        }

        private void UpdatePDAUserInterface()
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = OriginalOwner.Name,
                IDOwner = ContainedID?.FullName,
                JobTitle = ContainedID?.JobTitle
            };

            _interface.SetState(new PDAUpdateUserInterfaceState(_lightOn,ownerInfo));
        }

        public bool AttackBy(AttackByEventArgs eventArgs)
        {
            var item = eventArgs.AttackWith;
            if (!IdSlotEmpty)
            {
                return false;
            }

            if (item.TryGetComponent<IdCardComponent>(out var idCardComponent) && !_idSlot.Contains(item))
            {
                _idSlot.Insert(item);
                ContainedID = idCardComponent;
                UpdatePDAUserInterface();
                return true;
            }

            return false;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return false;
            }
            _interface.Open(actor.playerSession);

            return true;
        }


        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

        }

        public void SetPDAOwner(IEntity mob)
        {
            if (mob == null || mob == OriginalOwner)
            {
                return;
            }

            OriginalOwner = mob;
            UpdatePDAUserInterface();
        }

        protected void ToggleLight()
        {
            _lightOn = !_lightOn;
            _pdaLight.Enabled = _lightOn;
            UpdatePDAUserInterface();
        }

        protected void HandleIDEjection(IEntity pdaUser)
        {
            if (IdSlotEmpty)
            {
                return;
            }

            var cardEntity = ContainedID.Owner;
            _idSlot.Remove(cardEntity);

            var hands = pdaUser.GetComponent<HandsComponent>();
            var cardItemComponent = cardEntity.GetComponent<ItemComponent>();
            hands.PutInHandOrDrop(cardItemComponent);
            ContainedID = null;
            UpdatePDAUserInterface();
        }


        [Verb]
        public sealed class EjectIDVerb : Verb<PDAComponent>
        {
            protected override string GetText(IEntity user, PDAComponent component)
            {
                return Loc.GetString("Eject ID");
            }

            protected override VerbVisibility GetVisibility(IEntity user, PDAComponent component)
            {
                return component.IdSlotEmpty ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, PDAComponent component)
            {
                component.HandleIDEjection(user);
            }
        }



    }
}
