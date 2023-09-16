using Content.Server.Construction.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ChangeConstructionGraph : IGraphAction
{
    [DataField("graph", required: true)]
    public string Graph = string.Empty;

    [DataField("node", required: true)]
    public string Node = string.Empty;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<ConstructionComponent>(uid, out var construction))
            return;

        if (entityManager.TrySystem(out ConstructionSystem? constructionSystem))
        {
            constructionSystem.ChangeGraph(uid, userUid, Graph, Node, true, construction);
        }
    }
}
