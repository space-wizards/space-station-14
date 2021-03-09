#nullable enable
using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Construction;
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
            if(!entity.TryGetComponent(out StackComponent? stackComponent)) return;

            stackComponent.Count = Math.Min(stackComponent.MaxCount, Amount);

            if (Amount > stackComponent.MaxCount)
            {
                Logger.Warning("StackCount is bigger than maximum stack capacity, for entity " + entity.Name);
            }
        }
    }
}
