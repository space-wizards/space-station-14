using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Access.Components;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.PDA.Managers;
using Content.Server.UserInterface;
using Content.Shared.Interaction;
using Content.Shared.ActionBlocker;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.PDA
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IAccess))]
    public class PDAComponent : SharedPDAComponent, IInteractUsing, IActivate, IUse, IAccess, IMapInit
    {
        [Dependency] private readonly IPDAUplinkManager _uplinkManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] public ContainerSlot IdSlot = default!;
        [ViewVariables] public ContainerSlot PenSlot = default!;

        [ViewVariables] private bool _lightOn;

        [ViewVariables] [DataField("idCard")] private string? _startingIdCard = "AssistantIDCard";
        [ViewVariables] [DataField("pen")] private string? _startingPen = "Pen";

        [ViewVariables] public string? OwnerName { get; private set; }

        [ViewVariables] public IdCardComponent? ContainedID { get; private set; }
        [ViewVariables] public bool IdSlotEmpty => IdSlot.ContainedEntity == null;
        [ViewVariables] public bool PenSlotEmpty => PenSlot.ContainedEntity == null;

        private UplinkAccount? _syndicateUplinkAccount;

        [ViewVariables] public UplinkAccount? SyndicateUplinkAccount => _syndicateUplinkAccount;

        [ViewVariables] private readonly PdaAccessSet _accessSet;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(PDAUiKey.Key);

        [DataField("insertIdSound")] private SoundSpecifier _insertIdSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/batrifle_magin.ogg");
        [DataField("toggleFlashlightSound")] private SoundSpecifier _toggleFlashlightSound = new SoundPathSpecifier("/Audio/Items/flashlight_pda.ogg");
        [DataField("ejectIdSound")] private SoundSpecifier _ejectIdSound = new SoundPathSpecifier("/Audio/Machines/id_swipe.ogg");

        public PDAComponent()
        {
            _accessSet = new PdaAccessSet(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            IdSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "pda_entity_container");
            PenSlot = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, "pda_pen_slot");

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }

            UpdatePDAAppearance();
        }

        public void MapInit()
        {
            if (!string.IsNullOrEmpty(_startingIdCard))
            {
                var idCard = _entityManager.SpawnEntity(_startingIdCard, Owner.Transform.Coordinates);
                var idCardComponent = idCard.GetComponent<IdCardComponent>();
                IdSlot.Insert(idCardComponent.Owner);
                ContainedID = idCardComponent;
            }

            if (!string.IsNullOrEmpty(_startingPen))
            {
                var pen = _entityManager.SpawnEntity(_startingPen, Owner.Transform.Coordinates);
                PenSlot.Insert(pen);
            }
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case PDARequestUpdateInterfaceMessage _:
                {
                    UpdatePDAUserInterface();
                    break;
                }
                case PDAToggleFlashlightMessage _:
                {
                    ToggleLight();
                    break;
                }

                case PDAEjectIDMessage _:
                {
                    HandleIDEjection(message.Session.AttachedEntity!);
                    break;
                }

                case PDAEjectPenMessage _:
                {
                    HandlePenEjection(message.Session.AttachedEntity!);
                    break;
                }

                case PDAUplinkBuyListingMessage buyMsg:
                {
                    var player = message.Session.AttachedEntity;
                    if (player == null) break;

                    if (!_uplinkManager.TryPurchaseItem(_syndicateUplinkAccount, buyMsg.ItemId,
                        player.Transform.Coordinates, out var entity))
                    {
                        SendNetworkMessage(new PDAUplinkInsufficientFundsMessage(), message.Session.ConnectedClient);
                        break;
                    }

                    if (!player.TryGetComponent(out HandsComponent? hands) ||
                        !entity.TryGetComponent(out ItemComponent? item))
                        break;

                    hands.PutInHandOrDrop(item);

                    SendNetworkMessage(new PDAUplinkBuySuccessMessage(), message.Session.ConnectedClient);
                    break;
                }
            }
        }

        public void UpdatePDAUserInterface()
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
                UserInterface?.SetState(new PDAUpdateState(_lightOn, !PenSlotEmpty, ownerInfo, accData, listings));
            }
            else
            {
                UserInterface?.SetState(new PDAUpdateState(_lightOn, !PenSlotEmpty, ownerInfo));
            }

            UpdatePDAAppearance();
        }

        private void UpdatePDAAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(PDAVisuals.FlashlightLit, _lightOn);
                appearance.SetData(PDAVisuals.IDCardInserted, !IdSlotEmpty);
            }
        }

        private bool TryInsertIdCard(InteractUsingEventArgs eventArgs, IdCardComponent idCardComponent)
        {
            var item = eventArgs.Using;
            if (IdSlot.Contains(item))
                return false;

            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-pda-ui-try-insert-id-card-no-hands"));
                return true;
            }

            IEntity? swap = null;
            if (!IdSlotEmpty)
            {
                // Swap
                swap = IdSlot.ContainedEntities[0];
            }

            if (!hands.Drop(item))
            {
                return true;
            }

            if (swap != null)
            {
                hands.PutInHand(swap.GetComponent<ItemComponent>());
            }

            InsertIdCard(idCardComponent);

            UpdatePDAUserInterface();
            return true;
        }

        public bool TryInsertPen(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;
            if (PenSlot.Contains(item))
                return false;

            if (!eventArgs.User.TryGetComponent(out IHandsComponent? hands))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-pda-ui-try-insert-pen-no-hands"));
                return true;
            }

            IEntity? swap = null;
            if (!PenSlotEmpty)
            {
                // Swap
                swap = PenSlot.ContainedEntities[0];
            }

            if (!hands.Drop(item))
            {
                return true;
            }

            if (swap != null)
            {
                hands.PutInHand(swap.GetComponent<ItemComponent>());
            }

            // Insert Pen
            PenSlot.Insert(item);

            UpdatePDAUserInterface();
            return true;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var item = eventArgs.Using;

            if (item.TryGetComponent<IdCardComponent>(out var idCardComponent))
            {
                return TryInsertIdCard(eventArgs, idCardComponent);
            }

            if (item.HasTag("Write"))
            {
                return TryInsertPen(eventArgs);
            }

            return false;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                return;
            }

            UserInterface?.Toggle(actor.PlayerSession);
            UpdatePDAAppearance();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out ActorComponent? actor))
            {
                return false;
            }

            UserInterface?.Toggle(actor.PlayerSession);
            UpdatePDAAppearance();
            return true;
        }

        public void SetPDAOwner(string name)
        {
            OwnerName = name;
            UpdatePDAUserInterface();
        }

        public void InsertIdCard(IdCardComponent card)
        {
            IdSlot.Insert(card.Owner);
            ContainedID = card;
            SoundSystem.Play(Filter.Pvs(Owner), _insertIdSound.GetSound(), Owner);
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

        public void ToggleLight()
        {
            if (!Owner.TryGetComponent(out PointLightComponent? light))
            {
                return;
            }

            _lightOn = !_lightOn;
            light.Enabled = _lightOn;
            SoundSystem.Play(Filter.Pvs(Owner), _toggleFlashlightSound.GetSound(), Owner);
            UpdatePDAUserInterface();
        }

        public void HandleIDEjection(IEntity pdaUser)
        {
            if (ContainedID == null)
            {
                return;
            }

            var cardEntity = ContainedID.Owner;
            IdSlot.Remove(cardEntity);

            var hands = pdaUser.GetComponent<HandsComponent>();
            var cardItemComponent = cardEntity.GetComponent<ItemComponent>();
            hands.PutInHandOrDrop(cardItemComponent);
            ContainedID = null;

            SoundSystem.Play(Filter.Pvs(Owner), _ejectIdSound.GetSound(), Owner);
            UpdatePDAUserInterface();
        }

        public void HandlePenEjection(IEntity pdaUser)
        {
            if (PenSlotEmpty)
                return;

            var pen = PenSlot.ContainedEntities[0];
            PenSlot.Remove(pen);

            var hands = pdaUser.GetComponent<HandsComponent>();
            var itemComponent = pen.GetComponent<ItemComponent>();
            hands.PutInHandOrDrop(itemComponent);

            UpdatePDAUserInterface();
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
            private static readonly HashSet<string> EmptySet = new();

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
