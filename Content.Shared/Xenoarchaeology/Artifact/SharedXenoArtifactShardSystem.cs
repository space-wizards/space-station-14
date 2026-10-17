using Content.Shared.NameIdentifier;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.Xenoarchaeology.Artifact;

public sealed partial class SharedXenoArtifactShardSystem : EntitySystem
{
    [Dependency] private SharedXenoArtifactSystem _artifact = default!;
    [Dependency] private EntityQuery<XenoArtifactComponent> _artifactQuery;
    [Dependency] private EntityQuery<XenoArtifactNodeComponent> _nodeQuery;
    [Dependency] private EntityQuery<NameIdentifierComponent> _nameQuery;

    public override void Initialize()
    {
        base.Initialize();
    }

    [SubscribeLocalEvent]
    private void UnlockingStageFinished(Entity<XenoArtifactShardComponent> ent, ref ArtifactUnlockingFinishedEvent args)
    {
        if (!_artifactQuery.TryComp(ent.Owner, out var artifactComponent) || _nameQuery.HasComp(ent.Owner))
            return;

        var nodes = _artifact.GetAllNodes((ent.Owner, artifactComponent));
        foreach (var node in nodes)
        {
            if (!_nodeQuery.TryComp(node, out var nodeComp))
                return;

            if (_nameQuery.TryComp(node, out var nameComp)) // get the name identifier of the node and put it on the shard for QoL of identifying what each shard does.
            {
                if (!_nameQuery.TryComp(ent.Owner, out var shardNameComp))
                {
                    var cloneNameComp = Factory.GetComponent<NameIdentifierComponent>(); //make a clone of that node name identifier
                    cloneNameComp.Group = ent.Comp.Group;
                    cloneNameComp.Identifier = nameComp.Identifier;
                    AddComp(ent.Owner, cloneNameComp);
                }
            }
        }
    }
}
