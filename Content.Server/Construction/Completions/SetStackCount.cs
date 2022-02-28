using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SetStackCount : IGraphAction
    {
        [DataField("amount")] public int Amount { get; } = 1;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            EntitySystem.Get<StackSystem>().SetCount(uid, Amount);
        }
    }
}
