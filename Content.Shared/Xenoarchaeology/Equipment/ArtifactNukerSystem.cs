using Content.Shared.Xenoarchaeology.Equipment.Components;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Interaction;
using Content.Shared.Xenoarchaeology.Artifact;
using System.Linq;

namespace Content.Shared.Xenoarchaeology.Equipment;

public sealed class ArtifactNukerSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoArtifactSystem _xenoSys = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<ArtifactNukerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
    }

    public void OnBeforeRangedInteract(Entity<ArtifactNukerComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Target is not null && args.CanReach && !args.Handled)
            args.Handled = TryNukeActiveArtifactNode(args.Target.Value);
    }


    public bool TryNukeActiveArtifactNode(Entity<XenoArtifactComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var xenoent = (ent.Owner, ent.Comp);

        var nodes = _xenoSys.GetActiveNodes(xenoent);

        foreach (var node in nodes)
        {
            var predecessorsChunks = _xenoSys.GetPredecessorNodes(xenoent, node)
                .Chunk(2)
                .ToList();

            foreach (var chunk in predecessorsChunks)
            {
                if (chunk.Length <= 1)
                    continue;

                _xenoSys.AddEdge(xenoent, chunk[1], chunk[2]);
            }

            _xenoSys.RemoveNode(ent, node.Owner);
        }

        return true;
    }
}
