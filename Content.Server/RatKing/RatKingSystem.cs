using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.RatKing
{
    public sealed class RatKingSystem : EntitySystem
    {
        [Dependency] private readonly ActionsSystem _action = default!;
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly TransformSystem _xform = default!;

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
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
                return;
            }
            args.Handled = true;
            _hunger.ModifyHunger(uid, -component.HungerPerArmyUse, hunger);
            Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates); //spawn the little mouse boi
        }

        /// <summary>
        /// uses hunger to release a specific amount of miasma into the air. This heals the rat king
        /// and his servants through a specific metabolism.
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
                _popup.PopupEntity(Loc.GetString("rat-king-too-hungry"), uid, uid);
                return;
            }
            args.Handled = true;
            _hunger.ModifyHunger(uid, -component.HungerPerDomainUse, hunger);

            _popup.PopupEntity(Loc.GetString("rat-king-domain-popup"), uid);

            var transform = Transform(uid);
            var indices = _xform.GetGridOrMapTilePosition(uid, transform);
            var tileMix = _atmos.GetTileMixture(transform.GridUid, transform.MapUid, indices, true);
            tileMix?.AdjustMoles(Gas.Miasma, component.MolesMiasmaPerDomain);
        }
    }

    public sealed partial class RatKingRaiseArmyActionEvent : InstantActionEvent
    {

    }

    public sealed partial class RatKingDomainActionEvent : InstantActionEvent
    {

    }
};
