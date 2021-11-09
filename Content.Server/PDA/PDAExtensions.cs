using System.Diagnostics.CodeAnalysis;
using Content.Server.Access.Components;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Robust.Shared.GameObjects;

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
        public static IdCardComponent? GetHeldId(this IEntity player)
        {
            IdCardComponent? firstIdInPda = null;

            if (player.TryGetComponent(out HandsComponent? hands))
            {
                foreach (var item in hands.GetAllHeldItems())
                {
                    if (firstIdInPda == null &&
                        item.Owner.TryGetComponent(out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        firstIdInPda = pda.ContainedID;
                    }

                    if (item.Owner.TryGetComponent(out IdCardComponent? card))
                    {
                        return card;
                    }
                }
            }

            if (firstIdInPda != null)
            {
                return firstIdInPda;
            }

            IdCardComponent? firstIdInInventory = null;

            if (player.TryGetComponent(out InventoryComponent? inventory))
            {
                foreach (var item in inventory.GetAllHeldItems())
                {
                    if (firstIdInInventory == null &&
                        item.TryGetComponent(out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        firstIdInInventory = pda.ContainedID;
                    }

                    if (item.TryGetComponent(out IdCardComponent? card))
                    {
                        return card;
                    }
                }
            }

            return firstIdInInventory;
        }

        /// <summary>
        ///     Gets the id that a player is holding in their hands or inventory.
        ///     Order: Hands > ID slot > PDA in ID slot
        /// </summary>
        /// <param name="player">The player to check in.</param>
        /// <param name="id">The id card component.</param>
        /// <returns>true if found, false otherwise.</returns>
        public static bool TryGetHeldId(this IEntity player, [NotNullWhen(true)] out IdCardComponent? id)
        {
            return (id = player.GetHeldId()) != null;
        }
    }
}
