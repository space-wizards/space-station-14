using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Dataset;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Pointing;
using Content.Shared.RatKing;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Shared.SubFloor;
using Content.Shared.Body;
using Content.Shared.Body.Components;

namespace Content.Server.RatKing
{
    /// <inheritdoc/>
    public sealed class RatKingSystem : SharedRatKingSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmos = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly HTNSystem _htn = default!;
        [Dependency] private readonly HungerSystem _hunger = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RatKingComponent, RatKingRaiseArmyActionEvent>(OnRaiseArmy);
            SubscribeLocalEvent<RatKingComponent, RatKingDomainActionEvent>(OnDomain);
            SubscribeLocalEvent<RatKingComponent, AfterPointedAtEvent>(OnPointedAt);
            SubscribeLocalEvent<RatKingComponent, AfterPointedArrowEvent>(OnPointedNearby);
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
            var servant = Spawn(component.ArmyMobSpawnId, Transform(uid).Coordinates);
            var comp = EnsureComp<RatKingServantComponent>(servant);
            comp.King = uid;
            Dirty(servant, comp);

            component.Servants.Add(servant);
            _npc.SetBlackboard(servant, NPCBlackboard.FollowTarget, new EntityCoordinates(uid, Vector2.Zero));
            UpdateServantNpc(servant, component.CurrentOrder);
        }

        /// <summary>
        /// uses hunger to release a specific amount of ammonia into the air. This heals the rat king
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
            var tileMix = _atmos.GetTileMixture(uid, excite: true);
            tileMix?.AdjustMoles(Gas.Ammonia, component.MolesAmmoniaPerDomain);
        }

        private void OnPointedAt(EntityUid uid, RatKingComponent component, ref AfterPointedAtEvent args)
        {
            if (component.CurrentOrder != RatKingOrderType.CheeseEm)
                return;

            foreach (var servant in component.Servants)
            {
                _npc.SetBlackboard(servant, NPCBlackboard.CurrentOrderedTarget, args.Pointed);
            }
        }

        /// <summary>
        /// Give leeway on how close you have to point to a target.
        /// Allows you to point near someone and still have them targeted
        /// </summary>
        private void OnPointedNearby(EntityUid uid, RatKingComponent component, ref AfterPointedArrowEvent args)
        {
            if (component.CurrentOrder != RatKingOrderType.CheeseEm)
                return;

            var arrow = args.PointedArrow;
            var arrowXform = Transform(arrow);

            var ents = _lookup.GetEntitiesInRange(arrow, component.PointingMargin);

            var target = EntityUid.Invalid;
            var minDistance = component.PointingMargin + 1f;

            foreach (var ent in ents)
            {
                if (ent == uid)
                    continue;

                // Remove ineligable targets
                if (!TryComp<DamageableComponent>(ent, out var _1) || !TryComp<BodyComponent>(ent, out var _2))
                    continue;

                if (TryComp<SubFloorHideComponent>(ent, out var _3) || TryComp<RatKingServantComponent>(ent, out var _4))
                    continue;

                if (!arrowXform.Coordinates.TryDistance(EntityManager, Transform(ent).Coordinates, out var distance))
                    continue;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    target = ent;
                }
            }

            if (target != EntityUid.Invalid){
                foreach (var servant in component.Servants)
                {
                    _npc.SetBlackboard(servant, NPCBlackboard.CurrentOrderedTarget, target);
                }
            }
        }

        public override void UpdateServantNpc(EntityUid uid, RatKingOrderType orderType)
        {
            base.UpdateServantNpc(uid, orderType);

            if (!TryComp<HTNComponent>(uid, out var htn))
                return;

            if (htn.Plan != null)
                _htn.ShutdownPlan(htn);

            _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);
            _htn.Replan(htn);
        }

        public override void DoCommandCallout(EntityUid uid, RatKingComponent component)
        {
            base.DoCommandCallout(uid, component);

            if (!component.OrderCallouts.TryGetValue(component.CurrentOrder, out var datasetId) ||
                !PrototypeManager.TryIndex<DatasetPrototype>(datasetId, out var datasetPrototype))
                return;

            var msg = Random.Pick(datasetPrototype.Values);
            _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, true);
        }
    }
}
