#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Body;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Content.Shared.Prototypes.Kitchen;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenSpikeComponent : Component, IActivate, ISuicideAct
    {
        public override string Name => "KitchenSpike";

        private int _meatParts;
        private string? _meatPrototype;
        private string _meatSource1p = "?";
        private string _meatSource0 = "?";

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            var victim = eventArgs.User.GetComponent<HandsComponent>().PulledObject?.Owner;

            var sprite = Owner.GetComponent<SpriteComponent>();

            if (victim == null)
            {
                if (_meatParts == 0)
                {
                    return;
                }
                _meatParts--;

                if (!string.IsNullOrEmpty(_meatPrototype))
                {
                    Owner.EntityManager.SpawnEntity(_meatPrototype, Owner.Transform.Coordinates);
                }

                if (_meatParts != 0)
                {
                    eventArgs.User.PopupMessage(_meatSource1p);
                }
                else
                {
                    sprite.LayerSetState(0, "spike");
                    eventArgs.User.PopupMessage(_meatSource0);
                }
                return;
            }
            else if (_meatParts > 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("The spike already has something on it, finish collecting its meat first!"));
                return;
            }

            if (!victim.TryGetComponent<ButcherableComponent>(out var food))
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("{0:theName} can't be butchered on the spike.", victim));
                return;
            }

            _meatPrototype = food.MeatPrototype;
            _meatParts = 5;
            _meatSource1p = Loc.GetString("You remove some meat from {0:theName}.", victim);
            _meatSource0 = Loc.GetString("You remove the last piece of meat from {0:theName}!", victim);

            sprite.LayerSetState(0, "spikebloody");

            Owner.PopupMessageEveryone(Loc.GetString("{0:theName} has forced {1:theName} onto the spike, killing them instantly!", eventArgs.User, victim));
            victim.Delete();
        }

        public SuicideKind Suicide(IEntity victim, IChatManager chat)
        {
            var othersMessage = Loc.GetString("{0:theName} has thrown themselves on a meat spike!", victim);
            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("You throw yourself on a meat spike!");
            victim.PopupMessage(selfMessage);

            return SuicideKind.Piercing;
        }
    }
}
