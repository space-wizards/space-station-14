using System.Diagnostics.CodeAnalysis;
using Content.Server.Access.Components;
using Content.Server.Hands.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.PDA
{
    public static class PdaExtensions
    {
        /// <summary>
        ///     Gets the id that a player is holding in their hands or inventory.
        ///     Order: Hands > ID slot > PDA in ID slot
        /// </summary>
        /// <param name="player">The player to check in.</param>
        /// <returns>The id card component.</returns>
        public static IdCardComponent? GetHeldId(this EntityUid player)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (entMan.TryGetComponent(player, out HandsComponent? hands))
            {
                foreach (var item in hands.GetAllHeldItems())
                {
                    if (entMan.TryGetComponent(item.Owner, out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        return pda.ContainedID;
                    }

                    if (entMan.TryGetComponent(item.Owner, out IdCardComponent? card))
                    {
                        return card;
                    }
                }
            }

            var invSystem = EntitySystem.Get<InventorySystem>();

            if (invSystem.TryGetContainerSlotEnumerator(player, out var enumerator))
            {
                while (enumerator.MoveNext(out var containerSlot))
                {
                    if(!containerSlot.ContainedEntity.HasValue) continue;

                    if (entMan.TryGetComponent(containerSlot.ContainedEntity.Value, out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        return pda.ContainedID;
                    }

                    if (entMan.TryGetComponent(containerSlot.ContainedEntity.Value, out IdCardComponent? card))
                    {
                        return card;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Gets the id that a player is holding in their hands or inventory.
        ///     Order: Hands > ID slot > PDA in ID slot
        /// </summary>
        /// <param name="player">The player to check in.</param>
        /// <param name="id">The id card component.</param>
        /// <returns>true if found, false otherwise.</returns>
        public static bool TryGetHeldId(this EntityUid player, [NotNullWhen(true)] out IdCardComponent? id)
        {
            return (id = player.GetHeldId()) != null;
        }
    }
}
