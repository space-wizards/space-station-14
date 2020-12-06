#nullable enable
using System;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Construction
{
    public static class StackHelpers
    {
        /// <summary>
        ///     Spawns a stack of a specified type given an amount.
        /// </summary>
        public static IEntity SpawnStack(StackType stack, int amount, EntityCoordinates coordinates, IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();

            string prototype;

            switch (stack)
            {
                case StackType.Metal:
                    prototype = "SteelSheet1";
                    break;

                case StackType.Glass:
                    prototype = "GlassSheet1";
                    break;

                case StackType.MetalRod:
                    prototype = "MetalRodStack1";
                    break;

                case StackType.Phoron:
                    prototype = "PhoronStack1";
                    break;

                case StackType.Plasteel:
                    prototype = "PlasteelSheet1";
                    break;

                case StackType.Cable:
                    prototype = "ApcExtensionCableStack1";
                    break;

                // TODO: Add more.

                default:
                    throw new ArgumentOutOfRangeException(nameof(stack),"Stack type doesn't have a prototype specified yet!");
            }

            var ent = entityManager.SpawnEntity(prototype, coordinates);

            var stackComponent = ent.GetComponent<StackComponent>();

            stackComponent.Count = Math.Min(amount, stackComponent.MaxCount);

            return ent;
        }
    }
}
