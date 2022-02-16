using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ConditionalAction : IGraphAction
    {
        [DataField("passUser")] public bool PassUser { get; } = false;

        [DataField("condition", required:true)] public IGraphCondition? Condition { get; } = null;

        [DataField("action", required:true)] public IGraphAction? Action { get; } = null;

        [DataField("else")] public IGraphAction? Else { get; } = null;

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
