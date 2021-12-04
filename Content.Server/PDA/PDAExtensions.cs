using System.Diagnostics.CodeAnalysis;
using Content.Server.Access.Components;
using Content.Server.Hands.Components;
using Content.Server.Inventory.Components;
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

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player, out HandsComponent? hands))
            {
                foreach (var item in hands.GetAllHeldItems())
                {
                    if (firstIdInPda == null &&
                        IoCManager.Resolve<IEntityManager>().TryGetComponent(item.Owner, out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        firstIdInPda = pda.ContainedID;
                    }

                    if (IoCManager.Resolve<IEntityManager>().TryGetComponent(item.Owner, out IdCardComponent? card))
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

            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(player, out InventoryComponent? inventory))
            {
                foreach (var item in inventory.GetAllHeldItems())
                {
                    if (firstIdInInventory == null &&
                        IoCManager.Resolve<IEntityManager>().TryGetComponent(item, out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        firstIdInInventory = pda.ContainedID;
                    }

                    if (IoCManager.Resolve<IEntityManager>().TryGetComponent(item, out IdCardComponent? card))
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
