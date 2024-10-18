using Content.Server.Stack;
using Content.Shared.Construction;
using Content.Shared.Stacks;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class SetStackCount : IGraphAction
    {
        [DataField("amount")] public int Amount { get; private set; } = 1;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (entityManager.TryGetComponent<StackComponent>(uid, out var stackComponent))
                entityManager.EntitySysManager.GetEntitySystem<StackSystem>().SetCount((uid, stackComponent), Amount);
        }
    }
}
