#nullable enable
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;

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
        private string _meatName = "?";

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            SpriteComponent? sprite;

            if (!EntitySystem.Get<SharedPullingSystem>().TryGetPulled(eventArgs.User, out var victim))
            {
                if (_meatParts == 0)
                {
                    return;
                }
                _meatParts--;

                if (!string.IsNullOrEmpty(_meatPrototype))
                {
                    var meat = Owner.EntityManager.SpawnEntity(_meatPrototype, Owner.Transform.Coordinates);
                    if (meat != null)
                    {
                        meat.Name = _meatName;
                    }
                }

                if (_meatParts != 0)
                {
                    eventArgs.User.PopupMessage(_meatSource1p);
                }
                else
                {
                    if (Owner.TryGetComponent(out sprite))
                    {
                        sprite.LayerSetState(0, "spike");
                    }

                    eventArgs.User.PopupMessage(_meatSource0);
                }

                return;
            }

            if (_meatParts > 0)
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
            // TODO: This could stand to be improved somehow, but it'd require Name to be much 'richer' in detail than it presently is.
            // But Name is RobustToolbox-level, so presumably it'd have to be done in some other way (interface???)
            _meatName = Loc.GetString("{0:name} meat", victim);

            if (Owner.TryGetComponent(out sprite))
            {
                sprite.LayerSetState(0, "spikebloody");
            }

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
