using Content.Server.DoAfter;
using Content.Server.Kitchen.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.MobState.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using System;
using static Content.Shared.Kitchen.Components.SharedKitchenSpikeComponent;

namespace Content.Server.Kitchen.EntitySystems
{
    internal class KitchenSpikeSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfter = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<KitchenSpikeComponent, DragDropEvent>(OnDragDrop);

            //DoAfter
            SubscribeLocalEvent<KitchenSpikeComponent, SpikingFinishedEvent>(OnSpikingFinished);
            SubscribeLocalEvent<KitchenSpikeComponent, SpikingFailEvent>(OnSpikingFail);
        }

        private void OnSpikingFail(EntityUid uid, KitchenSpikeComponent component, SpikingFailEvent args)
        {
            component.InUse = false;

            if (EntityManager.TryGetComponent<SharedButcherableComponent>(args.VictimUid, out var butcherable))
                butcherable.BeingButchered = false;
        }

        private void OnSpikingFinished(EntityUid uid, KitchenSpikeComponent component, SpikingFinishedEvent args)
        {
            component.InUse = false;

            if (EntityManager.TryGetComponent<SharedButcherableComponent>(args.VictimUid, out var butcherable))
                butcherable.BeingButchered = false;

            if (Spikeable(uid, args.UserUid, args.VictimUid, component, butcherable))
            {
                Spike(uid, args.UserUid, args.VictimUid, component);
            }
        }

        private void OnDragDrop(EntityUid uid, KitchenSpikeComponent component, DragDropEvent args)
        {
            if(args.Handled)
                return;

            if (!Spikeable(uid, args.User, args.Dragged, component))
                return;

            if (TrySpike(uid, args.User, args.Dragged, component))
                args.Handled = true;
        }
        private void OnInteractHand(EntityUid uid, KitchenSpikeComponent component, InteractHandEvent args)
        {
            if (args.Handled)
                return;

            if (component.MeatParts > 0) {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-knife-needed"), uid, Filter.Entities(args.User));
                args.Handled = true;
            }
        }

        private void OnInteractUsing(EntityUid uid, KitchenSpikeComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;
            
            if (TryGetPiece(uid, args.User, args.Used))
                args.Handled = true;
        }

        private void Spike(EntityUid uid, EntityUid userUid, EntityUid victimUid,
            KitchenSpikeComponent? component = null, SharedButcherableComponent? butcherable = null)
        {
            if (!Resolve(uid, ref component) || !Resolve(victimUid, ref butcherable))
                return;

            component.MeatPrototype = butcherable.MeatPrototype;
            component.MeatParts = butcherable.Pieces;

            // This feels not okay, but entity is getting deleted on "Spike", for now...
            component.MeatSource1p = Loc.GetString("comp-kitchen-spike-remove-meat", ("victim", victimUid));
            component.MeatSource0 = Loc.GetString("comp-kitchen-spike-remove-meat-last", ("victim", victimUid));
            component.MeatName = Loc.GetString("comp-kitchen-spike-meat-name", ("victim", victimUid));

            UpdateAppearance(uid, null, component);

            _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-kill", ("user", userUid), ("victim", victimUid)), uid, Filter.Pvs(userUid));

            // THE WHAT?
            // TODO: Need to be able to leave them on the spike to do DoT, see ss13.
            EntityManager.QueueDeleteEntity(victimUid);

            SoundSystem.Play(Filter.Pvs(uid), component.SpikeSound.GetSound(), uid);
        }

        private bool TryGetPiece(EntityUid uid, EntityUid user, EntityUid used,
            KitchenSpikeComponent? component = null, UtensilComponent? utensil = null)
        {
            if (!Resolve(uid, ref component) || component.MeatParts == 0)
                return false;

            // Is using knife
            if (!Resolve(used, ref utensil, false) || (utensil.Types & UtensilType.Knife) == 0)
            {
                return false;
            }

            component.MeatParts--;

            if (!string.IsNullOrEmpty(component.MeatPrototype))
            {
                var meat = EntityManager.SpawnEntity(component.MeatPrototype, Transform(uid).Coordinates);
                MetaData(meat).EntityName = component.MeatName;
            }

            if (component.MeatParts != 0)
            {
                _popupSystem.PopupEntity(component.MeatSource1p, uid, Filter.Entities(user));
            }
            else
            {
                UpdateAppearance(uid, null, component);
                _popupSystem.PopupEntity(component.MeatSource0, uid, Filter.Entities(user));
            }

            return true;
        }

        private void UpdateAppearance(EntityUid uid, AppearanceComponent? appearance = null, KitchenSpikeComponent? component = null)
        {
            if (!Resolve(uid, ref component, ref appearance, false))
                return;
            
            appearance.SetData(KitchenSpikeVisuals.Status, (component.MeatParts > 0) ? KitchenSpikeStatus.Bloody : KitchenSpikeStatus.Empty);
        }

        private bool Spikeable(EntityUid uid, EntityUid userUid, EntityUid victimUid,
            KitchenSpikeComponent? component = null, SharedButcherableComponent? butcherable = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (component.MeatParts > 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-collect", ("this", uid)), uid, Filter.Entities(userUid));
                return false;
            }

            if (!Resolve(victimUid, ref butcherable, false) || butcherable.MeatPrototype == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-butcher", ("victim", victimUid), ("this", uid)), victimUid, Filter.Entities(userUid));
                return false;
            }

            return true;
        }

        public bool TrySpike(EntityUid uid, EntityUid userUid, EntityUid victimUid, KitchenSpikeComponent? component = null,
            SharedButcherableComponent? butcherable = null, MobStateComponent? mobState = null)
        {
            if (!Resolve(uid, ref component) || component.InUse ||
                !Resolve(victimUid, ref butcherable) || butcherable.BeingButchered)
                return false;

            // THE WHAT? (again)
            // Prevent dead from being spiked TODO: Maybe remove when rounds can be played and DOT is implemented
            if (Resolve(victimUid, ref mobState, false) &&
                !mobState.IsDead())
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-not-dead", ("victim", victimUid)),
                    victimUid, Filter.Entities(userUid));
                return true;
            }

            if (userUid != victimUid)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-begin-hook-victim", ("user", userUid), ("this", uid)), victimUid, Filter.Entities(victimUid));
            }
            // TODO: make it work when SuicideEvent is implemented
            // else
            //    _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-begin-hook-self", ("this", uid)), victimUid, Filter.Pvs(uid)); // This is actually unreachable and should be in SuicideEvent

            butcherable.BeingButchered = true;
            component.InUse = true;

            var doAfterArgs = new DoAfterEventArgs(userUid, component.SpikeDelay, default, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                TargetFinishedEvent = new SpikingFinishedEvent(userUid, victimUid),
                TargetCancelledEvent = new SpikingFailEvent(victimUid)
            };

            _doAfter.DoAfter(doAfterArgs);

            return true;
        }

        private class SpikingFinishedEvent : EntityEventArgs
        {
            public EntityUid VictimUid;
            public EntityUid UserUid;

            public SpikingFinishedEvent(EntityUid userUid, EntityUid victimUid)
            {
                UserUid = userUid;
                VictimUid = victimUid;
            }
        }

        private class SpikingFailEvent : EntityEventArgs
        {
            public EntityUid VictimUid;

            public SpikingFailEvent(EntityUid victimUid)
            {
                VictimUid = victimUid;
            }
        }
    }
}
