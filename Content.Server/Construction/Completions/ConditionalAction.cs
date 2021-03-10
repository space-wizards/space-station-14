#nullable enable
using System.Threading.Tasks;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ConditionalAction : IGraphAction
    {
        [field: DataField("passUser")] public bool PassUser { get; } = false;

        [field: DataField("condition", required:true)] public IEdgeCondition? Condition { get; } = null;

        [field: DataField("action", required:true)] public IGraphAction? Action { get; } = null;

        [field: DataField("else")] public IGraphAction? Else { get; } = null;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (Condition == null || Action == null)
                return;

            if (await Condition.Condition(PassUser && user != null ? user : entity))
                await Action.PerformAction(entity, user);
            else if (Else != null)
                await Else.PerformAction(entity, user);
        }
    }
}
