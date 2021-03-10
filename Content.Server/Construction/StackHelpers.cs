#nullable enable
using System;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components;
using Content.Shared.Stacks;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Construction
{
    public static class StackHelpers
    {
        /// <summary>
        ///     Spawns a stack of a specified type given an amount.
        /// </summary>
        public static IEntity SpawnStack(StackPrototype stack, int amount, EntityCoordinates coordinates, IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            // TODO: Add more.
            string prototype = stack.Spawn ?? throw new ArgumentOutOfRangeException(nameof(stack),
                "Stack type doesn't have a prototype specified yet!");

            var ent = entityManager.SpawnEntity(prototype, coordinates);
            var stackComponent = ent.GetComponent<StackComponent>();

            stackComponent.Count = Math.Min(amount, stackComponent.MaxCount);

            return ent;
        }
    }
}
