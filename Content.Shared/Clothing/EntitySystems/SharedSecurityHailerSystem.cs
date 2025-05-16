using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Coordinates;
using Content.Shared.Stealth.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;

namespace Content.Shared.Clothing.EntitySystems
{
    public abstract class SharedSecurityHailerSystem : EntitySystem
    {
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedToolSystem _toolSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SecurityHailerComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<SecurityHailerComponent, ClothingGotEquippedEvent>(OnEquip);
            SubscribeLocalEvent<SecurityHailerComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnEquip(EntityUid uid, SecurityHailerComponent comp, ClothingGotEquippedEvent args)
        {
            _actions.AddAction(args.Wearer, ref comp.ActionEntity, comp.Action, uid);
        }

        private void OnMapInit(Entity<SecurityHailerComponent> ent, ref MapInitEvent args)
        {
            //COPY PASTED, IS THIS GOOD ?
            var (uid, comp) = ent;
            // test funny
            if (string.IsNullOrEmpty(comp.Action))
                return;

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
                if (iterator.Owner == uid)
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
                OnInteractCutting(ent, args);
            else if (_toolSystem.HasQuality(args.Used, SharedToolSystem.ScrewQuality))
                OnInteractScrewing(ent, args);
            else if (false) //TODO: ADD EMAG
                OnInteractEmag(ent, args);
            else
                return;
        }
        private void OnInteractCutting(Entity<SecurityHailerComponent> ent, InteractUsingEvent args)
        {
            throw new NotImplementedException();
        }

        private void OnInteractScrewing(Entity<SecurityHailerComponent> ent, InteractUsingEvent args)
        {
            //If it's emagged we don't change it
            if (ent.Comp.Emagged)
                return;
            var comp = ent.Comp;

            if (comp.AggresionLevel == SecurityHailerComponent.AggresionState.High)
                comp.AggresionLevel = SecurityHailerComponent.AggresionState.Low;
            else
                comp.AggresionLevel++;
        }


        private void OnInteractEmag(Entity<SecurityHailerComponent> ent, InteractUsingEvent args)
        {
            throw new NotImplementedException();
        }
    }
}
