using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Wires;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class RemoveWiresPanelSecurity : IGraphAction
{
    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<WiresPanelSecurityComponent>(uid, out var wiresPanelSecurity))
            return;

        if (!entityManager.TryGetComponent<ConstructionComponent>(uid, out var construction))
            return;

        if (entityManager.TrySystem(out ConstructionSystem? constructionSystem))
        {
            constructionSystem.ChangeGraph(uid, userUid, wiresPanelSecurity.BaseGraph, wiresPanelSecurity.BaseNode, true, construction);
        }
    }
}
