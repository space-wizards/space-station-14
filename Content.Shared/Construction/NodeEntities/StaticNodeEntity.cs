using JetBrains.Annotations;

namespace Content.Shared.Construction.NodeEntities;

[UsedImplicitly]
[DataDefinition]
public sealed class StaticNodeEntity : IGraphNodeEntity
{
    [DataField("id")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? Id { get; }

    public StaticNodeEntity()
    {
    }

    public StaticNodeEntity(string id)
    {
        Id = id;
    }

    public string? GetId(EntityUid? uid, EntityUid? userUid, GraphNodeEntityArgs args)
    {
        return Id;
    }
}
