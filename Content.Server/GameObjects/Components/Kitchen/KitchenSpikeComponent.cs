#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Kitchen;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenSpikeComponent : SharedKitchenSpikeComponent, IActivate, ISuicideAct
    {
        private int _meatParts;
        private string? _meatPrototype;
        private string _meatSource1p = "?";
        private string _meatSource0 = "?";
        private string _meatName = "?";

        private List<EntityUid> _beingButchered = new();

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            SpriteComponent? sprite;

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

        public override bool DragDropOn(DragDropEventArgs eventArgs)
        {
            TrySpike(eventArgs.Dragged, eventArgs.User);
            return true;
        }

        private bool Spikeable(IEntity user, IEntity victim, [NotNullWhen(true)] out SharedButcherableComponent? butcherable)
        {
            butcherable = null;

            if (_meatParts > 0)
            {
                Owner.PopupMessage(user, Loc.GetString("The spike already has something on it, finish collecting its meat first!"));
                return false;
            }

            if (!victim.TryGetComponent(out butcherable))
            {
                Owner.PopupMessage(user, Loc.GetString("{0:theName} can't be butchered on the spike.", victim));
                return false;
            }

            return true;
        }

        public async void TrySpike(IEntity victim, IEntity user)
        {
            var victimUid = victim.Uid;
            if (_beingButchered.Contains(victimUid)) return;

            SharedButcherableComponent? butcherable;

            if (!Spikeable(user, victim, out butcherable)) return;

            if (user != victim)
            {
                Owner.PopupMessage(victim, Loc.GetString("{0:theName} begins dragging you onto {1:theName}!", user, Owner));
            }
            else
            {
                Owner.PopupMessage(user, Loc.GetString("You begin dragging yourself onto {0:theName}!", Owner));
            }

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

            var doAfterArgs = new DoAfterEventArgs(user, SpikeDelay, default, victim)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
            };

            _beingButchered.Add(victimUid);

            var result = await doAfterSystem.DoAfter(doAfterArgs);

            _beingButchered.Remove(victimUid);

            if (result == DoAfterStatus.Cancelled)
                return;

            if (!Spikeable(user, victim, out butcherable)) return;

            _meatPrototype = butcherable.MeatPrototype;
            _meatParts = 5;
            _meatSource1p = Loc.GetString("You remove some meat from {0:theName}.", victim);
            _meatSource0 = Loc.GetString("You remove the last piece of meat from {0:theName}!", victim);
            // TODO: This could stand to be improved somehow, but it'd require Name to be much 'richer' in detail than it presently is.
            // But Name is RobustToolbox-level, so presumably it'd have to be done in some other way (interface???)
            _meatName = Loc.GetString("{0:name} meat", victim);

            // TODO: Visualizer
            if (Owner.TryGetComponent<SpriteComponent>(out var sprite))
            {
                sprite.LayerSetState(0, "spikebloody");
            }

            Owner.PopupMessageEveryone(Loc.GetString("{0:theName} has forced {1:theName} onto the spike, killing them instantly!", user, victim));
            // TODO: Need to be able to leave them on the spike to do DoT, see ss13.
            victim.Delete();

            if (SpikeSound != null)
                SoundSystem.Play(Filter.Pvs(Owner), SpikeSound, Owner);
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            var othersMessage = Loc.GetString("{0:theName} has thrown themselves on a meat spike!", victim);
            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("You throw yourself on a meat spike!");
            victim.PopupMessage(selfMessage);

            return SuicideKind.Piercing;
        }
    }
}
