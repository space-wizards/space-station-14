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
            SubscribeLocalEvent<KitchenSpikeComponent, DragDropEvent>(OnDragDrop);

            //DoAfter
            SubscribeLocalEvent<KitchenSpikeComponent, SpikingFinishedEvent>(OnSpiknigFinished);
            SubscribeLocalEvent<KitchenSpikeComponent, SpikingFailEvent>(OnSpikingFail);
        }

        private void OnSpikingFail(EntityUid uid, KitchenSpikeComponent component, SpikingFailEvent args)
        {
            if (EntityManager.TryGetComponent<SharedButcherableComponent>(args.VictimUid, out var butcherable))
                butcherable.BeingButchered = false;
        }

        private void OnSpiknigFinished(EntityUid uid, KitchenSpikeComponent component, SpikingFinishedEvent args)
        {
            if (EntityManager.TryGetComponent<SharedButcherableComponent>(args.VictimUid, out var butcherable))
                butcherable.BeingButchered = false;

            if (Spikeable(uid, args.UserUid, args.VictimUid, component, butcherable))
            {
                Spike(uid, args.UserUid, args.VictimUid, component);
            }
        }

        private void OnDragDrop(EntityUid uid, KitchenSpikeComponent component, DragDropEvent args)
        {
            // TODO: Server-side DragDropEvent should be Handleable and use UIDs
            // if(args.Handled)
            //      return;
            //
            // (!EntityManager.TryGetComponent<SharedButcherableComponent>(args.DraggedUid, out var butcherable)

            if (!EntityManager.TryGetComponent<SharedButcherableComponent>(args.Dragged.Uid, out var butcherable)
                || butcherable.BeingButchered)
                return;

            if (!Spikeable(uid, args.User.Uid, args.Dragged.Uid, component))
                return;

            // if (TrySpike(uid, args.DraggedUid, args.UserUid))
            //      args.Handled = true;

            TrySpike(uid, args.User.Uid, args.Dragged.Uid, component, butcherable);
        }

        private void OnInteractUsing(EntityUid uid, KitchenSpikeComponent component, InteractUsingEvent args)
        {
            if (args.Handled)
                return;
            
            if (TryGetPiece(uid, args.UserUid, args.UsedUid))
                args.Handled = true;
        }

        private void Spike(EntityUid uid, EntityUid userUid, EntityUid victimUid,
            KitchenSpikeComponent? component = null, SharedButcherableComponent? butcherable = null)
        {
            if (!Resolve(uid, ref component) || !Resolve(victimUid, ref butcherable))
                return;

            component.MeatPrototype = butcherable.MeatPrototype;
            component.MeatParts = butcherable.Pieces;
            component.MeatSource1p = Loc.GetString("comp-kitchen-spike-remove-meat", ("victim", victimUid));
            component.MeatSource0 = Loc.GetString("comp-kitchen-spike-remove-meat-last", ("victim", victimUid));
            // TODO: This could stand to be improved somehow, but it'd require Name to be much 'richer' in detail than it presently is.
            // But Name is RobustToolbox-level, so presumably it'd have to be done in some other way (interface???)
            component.MeatName = Loc.GetString("comp-kitchen-spike-meat-name", ("victim", victimUid));

            UpdateAppearance(uid, null, component);

            // TODO: for everyone
            _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-kill", ("user", userUid), ("victim", victimUid)), uid, Filter.Entities(userUid));

            // THE WHAT? (again)
            // TODO: Need to be able to leave them on the spike to do DoT, see ss13.
            EntityManager.QueueDeleteEntity(victimUid);

            // TODO: for everyone
            SoundSystem.Play(Filter.Pvs(userUid), component.SpikeSound.GetSound(), uid);
        }

        private bool TryGetPiece(EntityUid uid, EntityUid user, EntityUid used,
            KitchenSpikeComponent? component = null, TransformComponent? transform = null, UtensilComponent? utensil = null)
        {
            if (!Resolve(uid, ref component, ref transform) || component.MeatParts == 0)
                return false;

            // Is using knife
            if (!Resolve(used, ref utensil) || (utensil.Types & UtensilType.Knife) == 0)
                return false;

            component.MeatParts--;

            if (!string.IsNullOrEmpty(component.MeatPrototype))
            {
                var meat = EntityManager.SpawnEntity(component.MeatPrototype, transform.Coordinates);
                meat.Name = component.MeatName;
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
            if (!Resolve(uid, ref component, ref appearance))
                return;
            
            appearance.SetData(KitchenSpikeVisuals.Status, (component.MeatParts > 0) ? KitchenSpikeStatus.Bloody : KitchenSpikeStatus.Empty);
        }

        private bool Spikeable(EntityUid uid, EntityUid user, EntityUid victim,
            KitchenSpikeComponent? component = null, SharedButcherableComponent? butcherable = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (component.MeatParts > 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-collect", ("this", uid)), uid, Filter.Entities(user));
                return false;
            }

            if (!Resolve(victim, ref butcherable) || butcherable.MeatPrototype == null)
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-butcher", ("victim", victim), ("this", uid)), victim, Filter.Entities(user));
                return false;
            }

            return true;
        }

        public void TrySpike(EntityUid uid, EntityUid user, EntityUid victim, KitchenSpikeComponent? component = null,
            SharedButcherableComponent? butcherable = null, MobStateComponent? mobState = null)
        {
            if (!Resolve(uid, ref component) || !Resolve(victim, ref butcherable))
                return;

            // THE WHAT?
            // Prevent dead from being spiked TODO: Maybe remove when rounds can be played and DOT is implemented
            if (Resolve(victim, ref mobState) &&
                !mobState.IsDead())
            {
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-deny-not-dead", ("victim", victim)), victim, Filter.Entities(user));
                return;
            }

            if (user != victim)
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-begin-hook-victim", ("user", user), ("this", uid)), victim, Filter.Entities(user));
            else
                _popupSystem.PopupEntity(Loc.GetString("comp-kitchen-spike-begin-hook-self", ("this", uid)), victim, Filter.Entities(user));

            butcherable.BeingButchered = true;

            var doAfterArgs = new DoAfterEventArgs(user, component.SpikeDelay, default, uid)
            {
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true,
                TargetFinishedEvent = new SpikingFinishedEvent(user, victim),
                TargetCancelledEvent = new SpikingFailEvent(victim)
            };

            _doAfter.DoAfter(doAfterArgs);
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
