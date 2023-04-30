using Content.Server.Construction.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;

namespace Content.Server.Construction.Completions;

[UsedImplicitly]
[DataDefinition]
public sealed class AppearanceChange : IGraphAction
{
    /// <summary>
    /// The appearance key to use.
    /// </summary>
    [DataField("key")]
    public Enum Key = ConstructionVisuals.Key;

    /// <summary>
    /// The enum data to set. If not specified, will set the data to the name of the current edges' target node
    /// (or the current node). This is because appearance changes are usually associated with reaching a new node.
    /// </summary>
    [DataField("data")]
    public Enum? Data;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            return;

        if (Data != null)
        {
            entityManager.System<AppearanceSystem>().SetData(uid, Key, Data, appearance);
            return;
        }

        var (node, edge) = entityManager.System<ConstructionSystem>().GetCurrentNodeAndEdge(uid);
        var nodeName = edge?.Target ?? node?.Name;

        if (nodeName != null)
            entityManager.System<AppearanceSystem>().SetData(uid, Key, nodeName, appearance);
    }
}
