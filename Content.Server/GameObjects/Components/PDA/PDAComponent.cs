#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.Interfaces;
using Content.Server.Interfaces.PDA;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.PDA;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.PDA
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IAccess))]
    public class PDAComponent : SharedPDAComponent, IInteractUsing, IActivate, IUse, IAccess
    {
        [Dependency] private readonly IPDAUplinkManager _uplinkManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private Container _idSlot = default!;
        [ViewVariables] private bool _lightOn;
        [ViewVariables] private string _startingIdCard = default!;
        [ViewVariables] public bool IdSlotEmpty => _idSlot.ContainedEntities.Count < 1;
        [ViewVariables] public string? OwnerName { get; private set; }

        [ViewVariables] public IdCardComponent? ContainedID { get; private set; }

        [ViewVariables] private UplinkAccount? _syndicateUplinkAccount;

        [ViewVariables] private readonly PdaAccessSet _accessSet;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(PDAUiKey.Key);

        public PDAComponent()
        {
            _accessSet = new PdaAccessSet(this);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _startingIdCard, "idCard", "AssistantIDCard");
        }

        public override void Initialize()
        {
            base.Initialize();
            _idSlot = ContainerManagerComponent.Ensure<Container>("pda_entity_container", Owner, out var existed);

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }

            var idCard = _entityManager.SpawnEntity(_startingIdCard, Owner.Transform.GridPosition);
            var idCardComponent = idCard.GetComponent<IdCardComponent>();
            _idSlot.Insert(idCardComponent.Owner);
            ContainedID = idCardComponent;
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
                    HandleIDEjection(message.Session.AttachedEntity!);
                    break;
                }

                case PDAUplinkBuyListingMessage buyMsg:
                {
                    if (!_uplinkManager.TryPurchaseItem(_syndicateUplinkAccount, buyMsg.ItemId))
                    {
                        SendNetworkMessage(new PDAUplinkInsufficientFundsMessage(), message.Session.ConnectedClient);
                        break;
                    }

                    SendNetworkMessage(new PDAUplinkBuySuccessMessage(), message.Session.ConnectedClient);
                    break;
                }
            }
        }

        private void UpdatePDAUserInterface()
        {
            var ownerInfo = new PDAIdInfoText
            {
                ActualOwnerName = OwnerName,
                IdOwner = ContainedID?.FullName,
                JobTitle = ContainedID?.JobTitle
            };

            //Do we have an account? If so provide the info.
            if (_syndicateUplinkAccount != null)
            {
                var accData = new UplinkAccountData(_syndicateUplinkAccount.AccountHolder,
                    _syndicateUplinkAccount.Balance);
                var listings = _uplinkManager.FetchListings.Values.ToArray();
                UserInterface?.SetState(new PDAUpdateState(_lightOn, ownerInfo, accData, listings));
            }
            else
            {
                UserInterface?.SetState(new PDAUpdateState(_lightOn, ownerInfo));
            }

            UpdatePDAAppearance();
        }

        private void UpdatePDAAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(PDAVisuals.FlashlightLit, _lightOn);
            }
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
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
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return;
            }

            UserInterface?.Open(actor.playerSession);
            UpdatePDAAppearance();
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out IActorComponent? actor))
            {
                return false;
            }

            UserInterface?.Open(actor.playerSession);
            UpdatePDAAppearance();
            return true;
        }

        public void SetPDAOwner(string name)
        {
            OwnerName = name;
            UpdatePDAUserInterface();
        }

        private void InsertIdCard(IdCardComponent card)
        {
            _idSlot.Insert(card.Owner);
            ContainedID = card;
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Weapons/Guns/MagIn/batrifle_magin.ogg", Owner);
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
            if (!Owner.TryGetComponent(out PointLightComponent? light))
            {
                return;
            }

            _lightOn = !_lightOn;
            light.Enabled = _lightOn;
            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/flashlight_toggle.ogg", Owner);
            UpdatePDAUserInterface();
        }

        private void HandleIDEjection(IEntity pdaUser)
        {
            if (ContainedID == null)
            {
                return;
            }

            var cardEntity = ContainedID.Owner;
            _idSlot.Remove(cardEntity);

            var hands = pdaUser.GetComponent<HandsComponent>();
            var cardItemComponent = cardEntity.GetComponent<ItemComponent>();
            hands.PutInHandOrDrop(cardItemComponent);
            ContainedID = null;

            EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Machines/id_swipe.ogg", Owner);
            UpdatePDAUserInterface();
        }

        [Verb]
        public sealed class EjectIDVerb : Verb<PDAComponent>
        {
            protected override void GetData(IEntity user, PDAComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Eject ID");
                data.Visibility = component.IdSlotEmpty ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, PDAComponent component)
            {
                component.HandleIDEjection(user);
            }
        }

        private ISet<string>? GetContainedAccess()
        {
            return ContainedID?.Owner?.GetComponent<AccessComponent>()?.Tags;
        }

        ISet<string> IAccess.Tags => _accessSet;
        bool IAccess.IsReadOnly => true;

        void IAccess.SetTags(IEnumerable<string> newTags)
        {
            throw new NotSupportedException("PDA access list is read-only.");
        }

        private sealed class PdaAccessSet : ISet<string>
        {
            private readonly PDAComponent _pdaComponent;
            private static readonly HashSet<string> EmptySet = new HashSet<string>();

            public PdaAccessSet(PDAComponent pdaComponent)
            {
                _pdaComponent = pdaComponent;
            }

            public IEnumerator<string> GetEnumerator()
            {
                var contained = _pdaComponent.GetContainedAccess() ?? EmptySet;
                return contained.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            void ICollection<string>.Add(string item)
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            public void ExceptWith(IEnumerable<string> other)
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            public void IntersectWith(IEnumerable<string> other)
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            public bool IsProperSubsetOf(IEnumerable<string> other)
            {
                var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
                return set.IsProperSubsetOf(other);
            }

            public bool IsProperSupersetOf(IEnumerable<string> other)
            {
                var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
                return set.IsProperSupersetOf(other);
            }

            public bool IsSubsetOf(IEnumerable<string> other)
            {
                var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
                return set.IsSubsetOf(other);
            }

            public bool IsSupersetOf(IEnumerable<string> other)
            {
                var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
                return set.IsSupersetOf(other);
            }

            public bool Overlaps(IEnumerable<string> other)
            {
                var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
                return set.Overlaps(other);
            }

            public bool SetEquals(IEnumerable<string> other)
            {
                var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
                return set.SetEquals(other);
            }

            public void SymmetricExceptWith(IEnumerable<string> other)
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            public void UnionWith(IEnumerable<string> other)
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            bool ISet<string>.Add(string item)
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            public void Clear()
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            public bool Contains(string item)
            {
                return _pdaComponent.GetContainedAccess()?.Contains(item) ?? false;
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                var set = _pdaComponent.GetContainedAccess() ?? EmptySet;
                set.CopyTo(array, arrayIndex);
            }

            public bool Remove(string item)
            {
                throw new NotSupportedException("PDA access list is read-only.");
            }

            public int Count => _pdaComponent.GetContainedAccess()?.Count ?? 0;
            public bool IsReadOnly => true;
        }
    }
}
