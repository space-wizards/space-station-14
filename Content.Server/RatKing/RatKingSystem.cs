using Content.Server.Actions;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Robust.Shared.Player;

namespace Content.Server.RatKing
{
    public sealed class RatKingSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RatKingComponent, ComponentStartup>(OnStartup);

            SubscribeLocalEvent<RatKingComponent, RatKingRaiseArmyActionEvent>(OnRaiseArmy);
            SubscribeLocalEvent<RatKingComponent, RatKingDomainActionEvent>(OnDomain);
        }

        private void OnStartup(EntityUid uid, RatKingComponent component, ComponentStartup args)
        {
            _action.AddAction(uid, component.ActionRaiseArmy, null);
            _action.AddAction(uid, component.ActionDomain, null);
        }

        /// <summary>
        /// Summons an allied rat servant at the King, costing a small amount of hunger
        /// </summary>
        private void OnRaiseArmy(EntityUid uid, RatKingComponent component, RatKingRaiseArmyActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            //make sure the hunger doesn't go into the negatives
            if (hunger.CurrentHunger < component.HungerPerArmyUse)
            {
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, Filter.Entities(uid));
                return;
            }
            args.Handled = true;
            hunger.CurrentHunger -= component.HungerPerArmyUse;
            Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates); //spawn the little mouse boi
        }

        /// <summary>
        /// Gets all of the nearby disease-carrying entities in a radius
        /// and gives them the specified disease. It has a hunger cost as well
        /// </summary>
        private void OnDomain(EntityUid uid, RatKingComponent component, RatKingDomainActionEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            //make sure the hunger doesn't go into the negatives
            if (hunger.CurrentHunger < component.HungerPerDomainUse)
            {
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, Filter.Entities(uid));
                return;
            }
            args.Handled = true;
            hunger.CurrentHunger -= component.HungerPerDomainUse;

            _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), uid, Filter.Pvs(uid, default, EntityManager));

            var tstalker = GetEntityQuery<DiseaseCarrierComponent>();
            foreach (var entity in _lookup.GetEntitiesInRange(uid, component.DomainRange)) //go through all of them, filtering only those in range that are not the king itself
            {
                if (entity == uid)
                    continue;

                if (tstalker.TryGetComponent(entity, out var diseasecomp))
                    _disease.TryInfect(diseasecomp, component.DomainDiseaseId); //infect them with w/e disease
            }
        }
    }

    public sealed class RatKingRaiseArmyActionEvent : InstantActionEvent { };
    public sealed class RatKingDomainActionEvent : InstantActionEvent { };
};
