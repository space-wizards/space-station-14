#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.Construction;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SetStackCount : IGraphAction
    {
        [DataField("amount")] public int Amount { get; private set; } = 1;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;
            if(!entity.HasComponent<StackComponent>()) return;

            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, new StackChangeCountEvent(Amount), false);
        }
    }
}
