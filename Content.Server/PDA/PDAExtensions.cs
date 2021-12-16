using System.Diagnostics.CodeAnalysis;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
using Content.Shared.Access.Components;
using Content.Shared.PDA;
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
            IdCardComponent? firstIdInPda = null;

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (entMan.TryGetComponent(player, out HandsComponent? hands))
            {
                foreach (var item in hands.GetAllHeldItems())
                {
                    if (firstIdInPda == null &&
                        entMan.TryGetComponent(item.Owner, out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        firstIdInPda = pda.ContainedID;
                    }

                    if (entMan.TryGetComponent(item.Owner, out IdCardComponent? card))
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

            if (entMan.TryGetComponent(player, out InventoryComponent? inventory))
            {
                foreach (var item in inventory.GetAllHeldItems())
                {
                    if (firstIdInInventory == null &&
                        entMan.TryGetComponent(item, out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        firstIdInInventory = pda.ContainedID;
                    }

                    if (entMan.TryGetComponent(item, out IdCardComponent? card))
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
        public static bool TryGetHeldId(this EntityUid player, [NotNullWhen(true)] out IdCardComponent? id)
        {
            return (id = player.GetHeldId()) != null;
        }
    }
}
