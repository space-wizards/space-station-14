using Content.Shared.Destructible.Thresholds;
using Robust.Shared.IoC;
using System.Collections.Generic;
using Content.Shared.Economy;
using Content.Shared.Procedural.Components;
using Robust.Shared.Random;

namespace Content.Server.Economy
{
    public sealed class ItemPriceManager : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        public Dictionary<EntityUid, int> CurrentPrices { get; private set; } = new();
        private readonly Dictionary<string, int> _prototypePrices = new();
        
        private readonly Dictionary<string, MinMax> _priceCategories = new()
        {
            ["food_cheap"] = new MinMax(2, 5),
            ["food_medium"] = new MinMax(5, 10),
            ["cigaretes"] = new MinMax(10, 15),
            ["drink"] = new MinMax(5, 10),
            ["tool_basic"] = new MinMax(25, 50),
            ["medical"] = new MinMax(15, 25),
            ["clothing"] = new MinMax(5, 10),
            ["miscellaneous"] = new MinMax(2, 6),
        };

        public void RecalculatePricesForRound()
        {
            CurrentPrices.Clear();
            _prototypePrices.Clear();

            var query = EntityQueryEnumerator<ItemPriceComponent>();

            while (query.MoveNext(out var uid, out var pricecomp))
            {
                if (!_priceCategories.TryGetValue(pricecomp.PriceCategory, out var minmax))
                {
                    continue;
                }
                int newprice = _random.Next(minmax.Min, minmax.Max + 1);
                CurrentPrices[uid] = newprice;
            }
        }

        /// <summary>
        /// Generate or retrieve a consistent price for a specific item prototype
        /// </summary>
        public int? GetPriceForPrototype(string prototypeId, string category)
        {
            if (_prototypePrices.TryGetValue(prototypeId, out var price))
            {
                return price;
            }

            if (!_priceCategories.TryGetValue(category, out var minMax))
            {
                return null;
            }

            var newPrice = _random.Next(minMax.Min, minMax.Max + 1);
            _prototypePrices[prototypeId] = newPrice;
            return newPrice;
        }
    }
}