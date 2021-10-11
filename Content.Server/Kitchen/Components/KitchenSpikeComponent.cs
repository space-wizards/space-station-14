using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Act;
using Content.Server.Chat.Managers;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Kitchen.Components;
using Content.Shared.MobState;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Kitchen.Components
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
                UpdateAppearance();

                eventArgs.User.PopupMessage(_meatSource0);
            }

            return;

        }

        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            TrySpike(eventArgs.Dragged, eventArgs.User);
            return true;
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(KitchenSpikeVisuals.Status, (_meatParts > 0) ? KitchenSpikeStatus.Bloody : KitchenSpikeStatus.Empty);
            }
        }

        private bool Spikeable(IEntity user, IEntity victim, [NotNullWhen(true)] out SharedButcherableComponent? butcherable)
        {
            butcherable = null;

            if (_meatParts > 0)
            {
                Owner.PopupMessage(user, Loc.GetString("comp-kitchen-spike-deny-collect", ("this", Owner)));
                return false;
            }

            if (!victim.TryGetComponent(out butcherable))
            {
                Owner.PopupMessage(user, Loc.GetString("comp-kitchen-spike-deny-butcher", ("victim", victim), ("this", Owner)));
                return false;
            }

            if (butcherable.MeatPrototype == null)
                return false;

            return true;
        }

        public async void TrySpike(IEntity victim, IEntity user)
        {
            var victimUid = victim.Uid;
            if (_beingButchered.Contains(victimUid)) return;

            SharedButcherableComponent? butcherable;

            if (!Spikeable(user, victim, out butcherable))
                return;

            // Prevent dead from being spiked TODO: Maybe remove when rounds can be played and DOT is implemented
            if (victim.TryGetComponent<IMobStateComponent>(out var state) &&
                !state.IsDead())
            {
                Owner.PopupMessage(user, Loc.GetString("comp-kitchen-spike-deny-not-dead", ("victim", victim)));
                return;
            }

            if (user != victim)
                Owner.PopupMessage(victim, Loc.GetString("comp-kitchen-spike-begin-hook-victim", ("user", user), ("this", Owner)));
            else
                Owner.PopupMessage(victim, Loc.GetString("comp-kitchen-spike-begin-hook-self", ("this", Owner)));

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

            var result = await doAfterSystem.WaitDoAfter(doAfterArgs);

            _beingButchered.Remove(victimUid);

            if (result == DoAfterStatus.Cancelled)
                return;

            if (!Spikeable(user, victim, out butcherable))
                return;

            _meatPrototype = butcherable.MeatPrototype;
            _meatParts = 5;
            _meatSource1p = Loc.GetString("comp-kitchen-spike-remove-meat", ("victim", victim));
            _meatSource0 = Loc.GetString("comp-kitchen-spike-remove-meat-last", ("victim", victim));
            // TODO: This could stand to be improved somehow, but it'd require Name to be much 'richer' in detail than it presently is.
            // But Name is RobustToolbox-level, so presumably it'd have to be done in some other way (interface???)
            _meatName = Loc.GetString("comp-kitchen-spike-meat-name", ("victim", victim));

            // TODO: Visualizer
            UpdateAppearance();

            Owner.PopupMessageEveryone(Loc.GetString("comp-kitchen-spike-kill", ("user", user), ("victim", victim)));
            // TODO: Need to be able to leave them on the spike to do DoT, see ss13.
            victim.Delete();

            SoundSystem.Play(Filter.Pvs(Owner), SpikeSound.GetSound(), Owner);
        }

        SuicideKind ISuicideAct.Suicide(IEntity victim, IChatManager chat)
        {
            var othersMessage = Loc.GetString("comp-kitchen-spike-suicide-other", ("victim", victim));
            victim.PopupMessageOtherClients(othersMessage);

            var selfMessage = Loc.GetString("comp-kitchen-spike-suicide-self");
            victim.PopupMessage(selfMessage);

            return SuicideKind.Piercing;
        }
    }
}
