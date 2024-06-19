using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Xenoarchaeology.Artifact;

public sealed partial class XenoArtifactSystem : SharedXenoArtifactSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoArtifactComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<XenoArtifactComponent, MapInitEvent>(OnArtifactMapInit);
    }

    private void OnGetState(Entity<XenoArtifactComponent> ent, ref ComponentGetState args)
    {
        var nodeVertices = new List<NetEntity?>();
        foreach (var vertex in ent.Comp.NodeVertices)
        {
            nodeVertices.Add(GetNetEntity(vertex));
        }

        var nodeAdjacencyMatrix = new List<List<bool>>();
        for (var i = 0; i < ent.Comp.NodeAdjacencyMatrixRows; i++)
        {
            var row = new List<bool>();
            for (var j = 0; j < ent.Comp.NodeAdjacencyMatrixColumns; j++)
            {
                row.Add(ent.Comp.NodeAdjacencyMatrix[i, j]);
            }

            nodeAdjacencyMatrix.Add(row);
        }

        args.State = new XenoArtifactComponentState(nodeVertices, nodeAdjacencyMatrix);
    }

    private void OnArtifactMapInit(Entity<XenoArtifactComponent> ent, ref MapInitEvent args)
    {
        GenerateArtifactStructure(ent);
    }
}
