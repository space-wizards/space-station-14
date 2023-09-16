using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class ChangeEntity : IGraphAction
{
    [DataField("prototype", required: true)]
    public string Prototype = string.Empty;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (Prototype == null)
            return;

        if (entityManager.TrySystem(out ConstructionSystem? constructionSystem))
        {
            constructionSystem.TryChangeEntity(uid, userUid, Prototype, out var _);
        }
    }
}
