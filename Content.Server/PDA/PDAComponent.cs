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
    [ComponentReference(typeof(IAccess))]
    public class PDAComponent : SharedPDAComponent, IAccess
    {
        [Dependency] private readonly IPDAUplinkManager _uplinkManager = default!;

        [ViewVariables] private bool _lightOn;

        [ViewVariables] [DataField("idCard")] public string? StartingIdCard;


        public const string IDSlotName = "pda_id_slot";

        [ViewVariables] public string? OwnerName { get; private set; }

        [ViewVariables] public IdCardComponent? ContainedID;
        [ViewVariables] public bool IdSlotEmpty => false;


        private UplinkAccount? _syndicateUplinkAccount;

        [ViewVariables] public UplinkAccount? SyndicateUplinkAccount => _syndicateUplinkAccount;

        [ViewVariables] private readonly PdaAccessSet _accessSet;

        [ViewVariables] public BoundUserInterface? UserInterface => Owner.GetUIOrNull(PDAUiKey.Key);

        [DataField("toggleFlashlightSound")] private SoundSpecifier _toggleFlashlightSound = new SoundPathSpecifier("/Audio/Items/flashlight_toggle.ogg");

        public PDAComponent()
        {
            _accessSet = new PdaAccessSet(this);
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

        public ISet<string>? GetContainedAccess()
        {
            return ContainedID?.Owner?.GetComponent<AccessComponent>()?.Tags;
        }

        ISet<string> IAccess.Tags => _accessSet;

        bool IAccess.IsReadOnly => true;

        void IAccess.SetTags(IEnumerable<string> newTags)
        {
            throw new NotSupportedException("PDA access list is read-only.");
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

                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                    return;

                var item = EntitySystem.Get<ItemSlotsSystem>().PeekItemInSlot(slots, "pda_pen_slot");
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

                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                    return;

                var item = EntitySystem.Get<ItemSlotsSystem>().PeekItemInSlot(slots, "pda_id_slot");
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
