using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Xenoarchaeology.Artifact;

/// <inheritdoc/>
public sealed class XenoArtifactSystem : SharedXenoArtifactSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoArtifactComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(Entity<XenoArtifactComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not XenoArtifactComponentState state)
            return;

        ResizeNodeGraph(ent, state.NodeVertices.Count);

        // Copy over node vertices
        for (var i = 0; i < state.NodeVertices.Count; i++)
        {
            ent.Comp.NodeVertices[i] = GetEntity(state.NodeVertices[i]);
        }

        for (var i = 0; i < ent.Comp.NodeAdjacencyMatrixRows; i++)
        {
            for (var j = 0; j < ent.Comp.NodeAdjacencyMatrixColumns; j++)
            {
                ent.Comp.NodeAdjacencyMatrix[i, j] = state.NodeAdjacencyMatrix[i][j];
            }
        }
    }
}
