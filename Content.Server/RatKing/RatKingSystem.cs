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

        private void OnRaiseArmy(EntityUid uid, RatKingComponent component, RatKingRaiseArmyActionEvent args)
        {
            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            if (hunger.CurrentHunger < component.HungerPerArmyUse)
            {
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, Filter.Entities(uid));
                return;
            }
            args.Handled = true;
            hunger.CurrentHunger -= component.HungerPerArmyUse;
            Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates);
        }

        private void OnDomain(EntityUid uid, RatKingComponent component, RatKingDomainActionEvent args)
        {
            if (!TryComp<HungerComponent>(uid, out var hunger))
                return;

            if (hunger.CurrentHunger < component.HungerPerDomainUse)
            {
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, Filter.Entities(uid));
                return;
            }
            args.Handled = true;
            hunger.CurrentHunger -= component.HungerPerDomainUse;

            var xformQuery = EntityQuery<TransformComponent, DiseaseCarrierComponent>(false);
            var bodyXform = Transform(uid);
            _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), uid, Filter.Pvs(uid));

            foreach (var query in xformQuery)
                if (bodyXform.Coordinates.InRange(EntityManager, query.Item1.Coordinates, component.DomainRange) && query.Item1.Owner != uid)
                    _disease.TryInfect(query.Item2, component.DomainDiseaseId); 
        }
    }

    public sealed class RatKingRaiseArmyActionEvent : InstantActionEvent { };
    public sealed class RatKingDomainActionEvent : InstantActionEvent { };
};
