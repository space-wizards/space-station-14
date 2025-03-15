using System.Linq;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.NodeContainer.NodeGroups
{
    public interface IPipeNet : INodeGroup, IGasMixtureHolder
    {
        /// <summary>
        ///     Causes gas in the PipeNet to react.
        /// </summary>
        void Update();
    }

    [NodeGroup(NodeGroupID.Pipe)]
    public sealed class PipeNet : BaseNodeGroup, IPipeNet
    {
        [ViewVariables] public GasMixture Air { get; set; } = new() {Temperature = Atmospherics.T20C};

        [ViewVariables] private AtmosphereSystem? _atmosphereSystem;
        [ViewVariables] private DamageableSystem? _damage;
        [ViewVariables] private IEntityManager? _entMan;
        [ViewVariables] private IRobustRandom? _random;

        public EntityUid? Grid { get; private set; }

        public override void Initialize(Node sourceNode, IEntityManager entMan)
        {
            base.Initialize(sourceNode, entMan);

            Grid = entMan.GetComponent<TransformComponent>(sourceNode.Owner).GridUid;

            if (Grid == null)
            {
                // This is probably due to a cannister or something like that being spawned in space.
                return;
            }

            _atmosphereSystem = entMan.EntitySysManager.GetEntitySystem<AtmosphereSystem>();
            _atmosphereSystem.AddPipeNet(Grid.Value, this);
            _damage = entMan.EntitySysManager.GetEntitySystem<DamageableSystem>();
            _entMan = entMan;
            _random = IoCManager.Resolve<IRobustRandom>();
        }

        /// <summary>
        /// Calculate pressure damage for pipe. There is no damage if the pressure is below MaxPressure,
        /// and damage scales exponentially beyond that.
        /// </summary>
        private int PressureDamage(PipeNode pipe)
        {
            const float tau = 10; // number of atmos ticks to break pipe at nominal overpressure
            var diff = pipe.Air.Pressure - pipe.MaxPressure;
            const float alpha = 100/tau;
            return diff > 0 ? (int)(alpha*float.Exp(diff / pipe.MaxPressure)) : 0;
        }

        public void Update()
        {
            _atmosphereSystem?.React(Air, this);

            // Check each pipe node for overpressure and apply damage if needed
            foreach (var node in Nodes)
            {
                if (node is PipeNode pipe && pipe.MaxPressure > 0)
                {
                    // Prefer damaging pipes that are already damaged. This means that only one pipe
                    // fails instead of the whole pipenet bursting at the same time.
                    const float baseChance = 0.5f;
                    float p = baseChance;
                    if (_entMan != null && _entMan.TryGetComponent<DamageableComponent>(pipe.Owner, out var damage))
                    {
                        p += (float)damage.TotalDamage * (1 - baseChance);
                    }

                    if (_random != null && _random.Prob(1-p))
                        continue;

                    int dam = PressureDamage(pipe);
                    if (dam > 0)
                    {
                        var dspec = new DamageSpecifier();
                        dspec.DamageDict.Add("Structural", dam);
                        _damage?.TryChangeDamage(pipe.Owner, dspec);
                    }
                }
            }
        }

        public override void LoadNodes(List<Node> groupNodes)
        {
            base.LoadNodes(groupNodes);

            foreach (var node in groupNodes)
            {
                var pipeNode = (PipeNode) node;
                Air.Volume += pipeNode.Volume;
            }
        }

        public override void RemoveNode(Node node)
        {
            base.RemoveNode(node);

            // if the node is simply being removed into a separate group, we do nothing, as gas redistribution will be
            // handled by AfterRemake(). But if it is being deleted, we actually want to remove the gas stored in this node.
            if (!node.Deleting || node is not PipeNode pipe)
                return;

            Air.Multiply(1f - pipe.Volume / Air.Volume);
            Air.Volume -= pipe.Volume;
        }

        public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
        {
            RemoveFromGridAtmos();

            var newAir = new List<GasMixture>(newGroups.Count());
            foreach (var newGroup in newGroups)
            {
                if (newGroup.Key is IPipeNet newPipeNet)
                    newAir.Add(newPipeNet.Air);
            }

            _atmosphereSystem?.DivideInto(Air, newAir);
        }

        private void RemoveFromGridAtmos()
        {
            if (Grid == null)
                return;

            _atmosphereSystem?.RemovePipeNet(Grid.Value, this);
        }

        public override string GetDebugData()
        {
            return @$"Pressure: { Air.Pressure:G3}
Temperature: {Air.Temperature:G3}
Volume: {Air.Volume:G3}";
        }
    }
}
