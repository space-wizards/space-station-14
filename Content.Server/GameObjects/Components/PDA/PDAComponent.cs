using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.PDA;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.PDA;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.PDA
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IAccess))]
    public class PDAComponent : SharedPDAComponent, IInteractUsing, IActivate, IUse, IAccess
    {
#pragma warning disable 649
        [Dependency] protected readonly IPDAUplinkManager _uplinkManager;
        [Dependency] protected readonly IEntityManager _entityManager;
#pragma warning restore 649

        private Container _idSlot;
        private PointLightComponent _pdaLight;
        private bool _lightOn = false;
        private BoundUserInterface _interface;
        private string _startingIdCard;
        public bool IdSlotEmpty => _idSlot.ContainedEntities.Count < 1;
        public IEntity OwnerMob { get; private set; }

        public IdCardComponent ContainedID { get; private set; }

        private AppearanceComponent _appearance;

        private UplinkAccount _syndicateUplinkAccount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _startingIdCard, "idCard", "AssistantIDCard");
        }

        public override void Initialize()
        {
            base.Initialize();
            _idSlot = ContainerManagerComponent.Ensure<Container>("pda_entity_container", Owner, out var existed);
            _pdaLight = Owner.GetComponent<PointLightComponent>();
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _interface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(PDAUiKey.Key);
            _interface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            var idCard = _entityManager.SpawnEntity(_startingIdCard, Owner.Transform.GridPosition);
            var idCardComponent = idCard.GetComponent<IdCardComponent>();
            InsertIdCard(idCardComponent);
            UpdatePDAAppearance();
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case PDARequestUpdateInterfaceMessage msg:
                {
                    UpdatePDAUserInterface();
                    break;
                }
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

                case PDAUplinkBuyListingMessage buyMsg:
                {

                    if (!_uplinkManager.TryPurchaseItem(_syndicateUplinkAccount, buyMsg.ListingToBuy))
                    {
                        //TODO: Send a message that tells the buyer they are too poor or something.
                    }

                    break;
                }
            }
        }

        private void UpdatePDAUserInterface()
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = OwnerMob?.Name,
                IdOwner = ContainedID?.FullName,
                JobTitle = ContainedID?.JobTitle
            };

            //Do we have an account? If so provide the info.
            if (_syndicateUplinkAccount != null)
            {
                var accData = new UplinkAccountData(_syndicateUplinkAccount.AccountHolder, _syndicateUplinkAccount.Balance);
                var listings = _uplinkManager.FetchListings.ToArray();
                _interface.SetState(new PDAUpdateState(_lightOn,ownerInfo,accData,listings));
            }
            else
            {
                _interface.SetState(new PDAUpdateState(_lightOn,ownerInfo));
            }

            UpdatePDAAppearance();
        }

        private void UpdatePDAAppearance()
        {
            _appearance?.SetData(PDAVisuals.ScreenLit, _lightOn);
        }

        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (!IdSlotEmpty)
            {
                return false;
            }

            if (!item.TryGetComponent<IdCardComponent>(out var idCardComponent) || _idSlot.Contains(item))
            {
                return false;
            }
            InsertIdCard(idCardComponent);
            UpdatePDAUserInterface();
            return true;

        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return;
            }
            _interface.Open(actor.playerSession);
            UpdatePDAAppearance();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent actor))
            {
                return false;
            }
            _interface.Open(actor.playerSession);
            UpdatePDAAppearance();
            return true;
        }

        public void SetPDAOwner(IEntity mob)
        {
            if (mob == OwnerMob)
            {
                return;
            }

            OwnerMob = mob;
            UpdatePDAUserInterface();
        }

        private void InsertIdCard(IdCardComponent card)
        {
            _idSlot.Insert(card.Owner);
            ContainedID = card;
        }

        /// <summary>
        /// Initialize the PDA's syndicate uplink account.
        /// </summary>
        /// <param name="acc"></param>
        public void InitUplinkAccount(UplinkAccount acc)
        {
            _syndicateUplinkAccount = acc;
            _uplinkManager.AddNewAccount(_syndicateUplinkAccount);

            _syndicateUplinkAccount.BalanceChanged += account =>
            {
                UpdatePDAUserInterface();
            };

            UpdatePDAUserInterface();
        }

        private void ToggleLight()
        {
            _lightOn = !_lightOn;
            _pdaLight.Enabled = _lightOn;
            UpdatePDAUserInterface();
        }

        private void HandleIDEjection(IEntity pdaUser)
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
            protected override void GetData(IEntity user, PDAComponent component, VerbData data)
            {
                data.Text = Loc.GetString("Eject ID");
                data.Visibility = component.IdSlotEmpty ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, PDAComponent component)
            {
                component.HandleIDEjection(user);
            }
        }

        List<string> IAccess.GetTags()
        {
            return ContainedID?.Owner.GetComponent<AccessComponent>()?.GetTags();
        }

        void IAccess.SetTags(List<string> newTags)
        {
            ContainedID?.Owner.GetComponent<AccessComponent>().SetTags(newTags);
        }
    }
}
