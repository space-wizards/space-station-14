using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Access.Components;
using Content.Server.Containers.ItemSlots;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.PDA.Managers;
using Content.Server.UserInterface;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Notification.Managers;
using Content.Shared.PDA;
using Content.Shared.Sound;
using Content.Shared.Tag;
using Content.Shared.Verbs;
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
    public class PDAComponent : SharedPDAComponent, IActivate, IUse, IAccess, IMapInit
    {
        [Dependency] private readonly IPDAUplinkManager _uplinkManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private bool _lightOn;

        [ViewVariables] [DataField("idCard")] private string? _startingIdCard = "AssistantIDCard";


        [ViewVariables] public string? OwnerName { get; private set; }

        [ViewVariables] public IdCardComponent? ContainedID { get; private set; }
        [ViewVariables] public bool IdSlotEmpty => false;


        private UplinkAccount? _syndicateUplinkAccount;

        [ViewVariables] public UplinkAccount? SyndicateUplinkAccount => _syndicateUplinkAccount;

        [ViewVariables] private readonly PdaAccessSet _accessSet;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(PDAUiKey.Key);

        [DataField("insertIdSound")] private SoundSpecifier _insertIdSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/batrifle_magin.ogg");
        [DataField("toggleFlashlightSound")] private SoundSpecifier _toggleFlashlightSound = new SoundPathSpecifier("/Audio/Items/flashlight_toggle.ogg");
        [DataField("ejectIdSound")] private SoundSpecifier _ejectIdSound = new SoundPathSpecifier("/Audio/Machines/id_swipe.ogg");

        public PDAComponent()
        {
            _accessSet = new PdaAccessSet(this);
        }

        protected override void Initialize()
        {
            base.Initialize();

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
                //_idSlot.Insert(idCardComponent.Owner);
                ContainedID = idCardComponent;
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
                    // TODO: fix id slot
                    //HandleIDEjection(message.Session.AttachedEntity!);
                    break;
                }

                case PDAEjectPenMessage _:
                {
                        // TODO: fix pen slot
                        //HandlePenEjection(message.Session.AttachedEntity!);
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
                // TODO: fix pen slot
                UserInterface?.SetState(new PDAUpdateState(_lightOn, false, ownerInfo, accData, listings));
            }
            else
            {
                // TODO: fix pen slot
                UserInterface?.SetState(new PDAUpdateState(_lightOn, false, ownerInfo));
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
            SoundSystem.Play(Filter.Pvs(Owner), _toggleFlashlightSound.GetSound(), Owner);
            UpdatePDAUserInterface();
        }

        [Verb]
        public sealed class ToggleFlashlightVerb : Verb<PDAComponent>
        {
            protected override void GetData(IEntity user, PDAComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("toggle-flashlight-verb-get-data-text");
                data.IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, PDAComponent component)
            {
                component.ToggleLight();
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


        // TODO: replace me with dynamic verbs for ItemSlotsSystem
        [Verb]
        public sealed class EjectPenVerb : Verb<PDAComponent>
        {
            protected override void GetData(IEntity user, PDAComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!component.Owner.TryGetComponent(out ItemSlotsComponent? slots))
                    return;

                var item = EntitySystem.Get<ItemSlotsSystem>().GetItemInSlot(slots, "pda_pen_slot", user);
                if (item == null)
                    return;

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("eject-item-verb-text-default", ("item", item.Name));
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, PDAComponent component)
            {
                if (!component.Owner.TryGetComponent(out ItemSlotsComponent? slots))
                    return;

                EntitySystem.Get<ItemSlotsSystem>().TryEjectContent(slots, "pda_pen_slot", user);
            }
        }

        // TODO: replace me with dynamic verbs for ItemSlotsSystem
        [Verb]
        public sealed class EjectIDVerb : Verb<PDAComponent>
        {
            public override bool AlternativeInteraction => true;

            protected override void GetData(IEntity user, PDAComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!component.Owner.TryGetComponent(out ItemSlotsComponent? slots))
                    return;

                var item = EntitySystem.Get<ItemSlotsSystem>().GetItemInSlot(slots, "pda_id_slot", user);
                if (item == null)
                    return;

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("eject-item-verb-text-default", ("item", item.Name));
                data.IconTexture = "/Textures/Interface/VerbIcons/eject.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, PDAComponent component)
            {
                if (!component.Owner.TryGetComponent(out ItemSlotsComponent? slots))
                    return;

                EntitySystem.Get<ItemSlotsSystem>().TryEjectContent(slots, "pda_id_slot", user);
            }
        }
    }
}
