using Content.Server.PowerCell;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Equipment;
using System.Linq;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <inheritdoc/>
public sealed class ArtifactNukerSystem : SharedArtifactNukerSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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
            var predecessors = _xenoSys.GetPredecessorNodes(ent.Owner, node)
                .ToList();

            var successors = _xenoSys.GetSuccessorNodes(ent.Owner, node)
                .ToList();

            foreach (var successor in successors)
            {
                if (_xenoSys.GetPredecessorNodes(ent.Owner, node).Count == 0)
                    _xenoSys.AddEdge(ent.Owner, successor, _random.Pick(successors));
                else
                    _xenoSys.AddEdge(ent.Owner, _random.Pick(predecessors), successor);
            }

            if (args.Nuker.Comp.ActivateNode)
            {
                var coords = Transform(ent).Coordinates;
                _xenoSys.ActivateNode(ent, node, args.User, null, coords);
            }

            _xenoSys.RemoveNode(ent.Owner, node.Owner);
        }
    }
}
