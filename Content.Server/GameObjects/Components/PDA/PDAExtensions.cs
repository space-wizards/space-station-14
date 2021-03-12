#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.PDA
{
    public static class PdaExtensions
    {
        /// <summary>
        ///     Gets the id that a player is holding in their hands or inventory.
        ///     Order: Hands > ID slot > PDA in ID slot
        /// </summary>
        /// <param name="player">The player to check in.</param>
        /// <returns>The id card component.</returns>
        public static IdCardComponent? PlayerGetId(this IEntity player)
        {
            if (player.TryGetComponent(out IHandsComponent? hands))
            {
                foreach (var item in hands.GetAllHeldItems())
                {
                    if (item.Owner.TryGetComponent(out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        return pda.ContainedID;
                    }

                    if (item.Owner.TryGetComponent(out IdCardComponent? card))
                    {
                        return card;
                    }
                }
            }

            if (player.TryGetComponent(out InventoryComponent? inventory))
            {
                foreach (var item in inventory.GetAllHeldItems())
                {
                    if (item.TryGetComponent(out PDAComponent? pda) &&
                        pda.ContainedID != null)
                    {
                        return pda.ContainedID;
                    }

                    if (item.TryGetComponent(out IdCardComponent? card))
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
        public static bool TryPlayerGetId(this IEntity player, [NotNullWhen(true)] out IdCardComponent? id)
        {
            return (id = player.PlayerGetId()) != null;
        }
    }
}
