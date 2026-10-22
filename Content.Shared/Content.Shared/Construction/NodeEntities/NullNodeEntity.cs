using JetBrains.Annotations;

namespace Content.Shared.Construction.NodeEntities;

[UsedImplicitly]
[DataDefinition]
public sealed partial class NullNodeEntity : IGraphNodeEntity
{
    public string? GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args)
    {
        return null;
    }
}
