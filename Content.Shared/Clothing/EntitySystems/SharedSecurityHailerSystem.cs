using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Stealth.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Swab;
using Content.Shared.Clothing.Event;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Clothing.EntitySystems
{
    public abstract class SharedSecurityHailerSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedToolSystem _toolSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedAudioSystem _sharedAudio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        private EntityUid _wearer;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecurityHailerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SecurityHailerComponent, ClothingGotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<SecurityHailerComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<SecurityHailerComponent, SecHailerToolDoAfterEvent>(OnScrewingDoAfter);
        }

        private void OnEquip(Entity<SecurityHailerComponent> ent, ref ClothingGotEquippedEvent args)
        {
            var (uid, comp) = ent;

            if (comp.CurrentState != SecMaskState.Functional)
                return;

            _wearer = args.Wearer;
            _actions.AddAction(args.Wearer, ref comp.ActionEntity, comp.Action, uid);
        }

        //In case someone spawns with it ?
        private void OnMapInit(Entity<SecurityHailerComponent> ent, ref MapInitEvent args)
        {
            //COPY PASTED, IS THIS GOOD ?
            var (uid, comp) = ent;
            // test funny
            if (string.IsNullOrEmpty(comp.Action))
                return;

            if (comp.CurrentState == SecMaskState.Functional)
                _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
            Dirty(uid, comp);
        }

        /// <summary>
        /// Put an exclamation mark around humanoid standing at the distance specified in the component.
        /// </summary>
        /// <param name="ent"></param>
        /// <returns></returns>
        protected bool ExclamateHumanoidsAround(Entity<SecurityHailerComponent> ent) //Put in shared for predictions purposes
        {
            var (uid, comp) = ent;
            if (!Resolve(uid, ref comp, false) || comp.Distance <= 0)
                return false;

            StealthComponent? stealth = null;
            foreach (var iterator in
                _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(_transform.GetMapCoordinates(uid), comp.Distance))
            {
                //Avoid pinging invisible entities
                if (TryComp(iterator, out stealth) && stealth.Enabled)
                    continue;

                //We don't want to ping user of whistle
                if (iterator.Owner == _wearer)
                    continue;

                SpawnAttachedTo(comp.ExclamationEffect, iterator.Owner.ToCoordinates());
            }

            return true;
        }

        private void OnInteractUsing(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            //Is it a wirecutter, a screwdriver or an EMAG ?
            if (_toolSystem.HasQuality(args.Used, SharedToolSystem.CutQuality))
                OnInteractCutting(ent, ref args);
            else if (_toolSystem.HasQuality(args.Used, SharedToolSystem.ScrewQuality))
                OnInteractScrewing(ent, ref args);
            else if (false) //TODO: ADD EMAG
                OnInteractEmag(ent, ref args);
            else
                return;
        }
        private void OnInteractCutting(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            // Snip, snip !
            _sharedAudio.PlayPvs(ent.Comp.CutSounds, ent.Owner);

            var (uid, comp) = ent;
            if (comp.CurrentState == SecMaskState.Functional)
            {
                comp.CurrentState = SecMaskState.WiresCut;
                _actions.RemoveAction(_wearer, comp.ActionEntity);
            }
            else if (comp.CurrentState == SecMaskState.WiresCut)
            {
                comp.CurrentState = SecMaskState.Functional ;
                //_actions.AddAction(_wearer, ref comp.ActionEntity, comp.Action, uid);
            }
            _appearance.SetData(ent, SecMaskVisuals.State, comp.CurrentState);
            args.Handled = true;
        }

        private void OnInteractScrewing(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            //If it's emagged we don't change it
            if (ent.Comp.Emagged)
                return;

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ScrewingDoAfterDelay, new SecHailerToolDoAfterEvent(), ent.Owner, target: args.Target, used: args.Used)
            {
                Broadcast = true,
                BreakOnMove = true,
                NeedHand = true,
            });

            args.Handled = true;
        }

        private void OnScrewingDoAfter(Entity<SecurityHailerComponent> ent, ref SecHailerToolDoAfterEvent args)
        {
            //Play a click sound just like the headset
            _sharedAudio.PlayPvs(ent.Comp.ScrewedSounds, ent.Owner);

            if (args.Cancelled || args.Handled || !TryComp<SecurityHailerComponent>(args.Args.Target, out var plant))
                return;

            var comp = ent.Comp;

            //Up the aggression level by one or back to one
            if (comp.AggresionLevel == SecurityHailerComponent.AggresionState.High)
                comp.AggresionLevel = SecurityHailerComponent.AggresionState.Low;
            else
                comp.AggresionLevel++;

            args.Handled = true;
        }


        private void OnInteractEmag(Entity<SecurityHailerComponent> ent, ref InteractUsingEvent args)
        {
            throw new NotImplementedException();
        }
    }

    
}
