using Content.Server.PowerCell;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using System.Linq;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <inheritdoc/>
public sealed class ArtifactNukerSystem : SharedArtifactNukerSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoArtifactComponent, AttemptNukeArtifact>(OnNukeArtifact);
    }

    public void OnNukeArtifact(Entity<XenoArtifactComponent> ent, ref AttemptNukeArtifact args)
    {
        if (!_powerCell.TryUseCharge(args.Nuker, args.Nuker.Comp.EnergyDrain, user: args.User))
            return;

        var nodes = _xenoSys.GetActiveNodes(ent);
        foreach (var node in nodes)
        {
            var index = _xenoSys.GetNodeId(node);
            if (args.Index != index)
                continue;

            var predecessors = _xenoSys.GetPredecessorNodes(ent.Owner, node)
                .ToList();

            var successors = _xenoSys.GetSuccessorNodes(ent.Owner, node)
                .ToList();

            for (var i = 0; i < predecessors.Count && i < successors.Count; i++)
                _xenoSys.AddEdge(ent.Owner, predecessors[i], successors[i]);

            if (args.Nuker.Comp.ActivateNode)
            {
                var coords = Transform(ent).Coordinates;
                _xenoSys.ActivateNode(ent, node, args.User, null, coords);
            }

            _xenoSys.RemoveNode(ent.Owner, node.Owner);
        }
    }
}
