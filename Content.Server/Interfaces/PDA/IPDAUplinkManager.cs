#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.PDA;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.PDA
{
    public interface IPDAUplinkManager
    {
        public IReadOnlyDictionary<string, UplinkListingData> FetchListings { get; }

        void Initialize();

        public bool AddNewAccount(UplinkAccount acc);

        public bool ChangeBalance(UplinkAccount acc, int amt);

        public bool TryPurchaseItem(
            UplinkAccount? acc,
            string itemId,
            EntityCoordinates spawnCoords,
            [NotNullWhen(true)] out IEntity? purchasedItem);

    }
}
