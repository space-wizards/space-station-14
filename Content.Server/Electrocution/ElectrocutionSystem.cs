using System;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.NodeGroups;
using Content.Server.Window;
using Content.Shared.Electrocution;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Weapons.Melee;
using Content.Shared.Window;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Random;

namespace Content.Server.Electrocution
{
    public sealed class ElectrocutionSystem : SharedElectrocutionSystem
    {
        [Dependency] private readonly IEntityLookup _entityLookup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ElectrifiedComponent, StartCollideEvent>(OnElectrifiedStartCollide);
            SubscribeLocalEvent<ElectrifiedComponent, AttackedEvent>(OnElectrifiedAttacked);
            SubscribeLocalEvent<ElectrifiedComponent, InteractHandEvent>(OnElectrifiedHandInteract);
            SubscribeLocalEvent<RandomInsulationComponent, MapInitEvent>(OnRandomInsulationMapInit);
        }

        private void OnElectrifiedStartCollide(EntityUid uid, ElectrifiedComponent electrified, StartCollideEvent args)
        {
            if (!electrified.OnBump)
                return;

            TryDoElectrifiedAct(uid, args.OtherFixture.Body.Owner.Uid, electrified);
        }

        private void OnElectrifiedAttacked(EntityUid uid, ElectrifiedComponent electrified, AttackedEvent args)
        {
            if (!electrified.OnAttacked)
                return;

            TryDoElectrifiedAct(uid, args.User.Uid, electrified);
        }

        private void OnElectrifiedHandInteract(EntityUid uid, ElectrifiedComponent electrified, InteractHandEvent args)
        {
            if (!electrified.OnHandInteract)
                return;

            TryDoElectrifiedAct(uid, args.User.Uid, electrified);
        }

        public bool TryDoElectrifiedAct(EntityUid uid, EntityUid targetUid,
            ElectrifiedComponent? electrified = null,
            NodeContainerComponent? nodeContainer = null,
            ITransformComponent? transform = null)
        {
            if(!Resolve(uid, ref electrified, ref transform, false))
                return false;

            if (!electrified.Enabled)
                return false;

            if (electrified.NoWindowInTile)
            {
                foreach (var entity in transform.Coordinates.GetEntitiesInTile(LookupFlags.Approximate | LookupFlags.IncludeAnchored, _entityLookup))
                {
                    if (entity.HasComponent<WindowComponent>())
                        return false;
                }

            }

            if(!electrified.RequirePower)
            {
                return TryDoElectrocution(targetUid, uid, electrified.ShockDamage,
                    TimeSpan.FromSeconds(electrified.ShockTime), electrified.SiemensCoefficient);
            }

            if(!Resolve(uid, ref nodeContainer, false))
                return false;

            // TODO: Right now this is very naive, we don't take any network parameters into account, just whether it's powered or not.
            // TODO: Because we support CableDeviceNode and CableNode, we need to use Node. That ain't great.
            if (electrified.HighVoltageNode is {} hv && nodeContainer.TryGetNode<Node>(hv, out var hvNode)
                                                     && hvNode.NodeGroup is PowerNet {NetworkNode: {LastAvailableSupplySum: > 0}})
            {
                return TryDoElectrocution(targetUid, uid, (int) (electrified.ShockDamage * electrified.HighVoltageDamageMultiplier),
                    TimeSpan.FromSeconds(electrified.ShockTime * electrified.HighVoltageTimeMultiplier), electrified.SiemensCoefficient);
            }

            if (electrified.MediumVoltageNode is {} mv && nodeContainer.TryGetNode<Node>(mv, out var mvNode)
                                                       && mvNode.NodeGroup is PowerNet {NetworkNode: {LastAvailableSupplySum: > 0}})
            {
                return TryDoElectrocution(targetUid, uid, (int) (electrified.ShockDamage * electrified.HighVoltageDamageMultiplier),
                    TimeSpan.FromSeconds(electrified.ShockTime * electrified.HighVoltageTimeMultiplier), electrified.SiemensCoefficient);
            }

            if (electrified.LowVoltageNode is {} lv && nodeContainer.TryGetNode<Node>(lv, out var lvNode)
                                                    && lvNode.NodeGroup is ApcNet {NetworkNode: {LastAvailableSupplySum: > 0}})
            {
                return TryDoElectrocution(targetUid, uid, electrified.ShockDamage,
                    TimeSpan.FromSeconds(electrified.ShockTime), electrified.SiemensCoefficient);
            }

            return false;
        }

        private void OnRandomInsulationMapInit(EntityUid uid, RandomInsulationComponent randomInsulation, MapInitEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out InsulatedComponent? insulated))
                return;

            if (randomInsulation.List.Length == 0)
                return;

            SetInsulatedSiemensCoefficient(uid, _random.Pick(randomInsulation.List), insulated);
        }
    }
}
