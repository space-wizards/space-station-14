using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ConditionalAction : IGraphAction
    {
        [DataField("passUser")] public bool PassUser { get; }

        [DataField("condition", required:true)] public IGraphCondition? Condition { get; }

        [DataField("action", required:true)] public IGraphAction? Action { get; }

        [DataField("else")] public IGraphAction? Else { get; }

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (Condition == null || Action == null)
                return;

            if (Condition.Condition(PassUser && userUid != null ? userUid.Value : uid, entityManager))
                Action.PerformAction(uid, userUid, entityManager);
            else
                Else?.PerformAction(uid, userUid, entityManager);
        }
    }
}
