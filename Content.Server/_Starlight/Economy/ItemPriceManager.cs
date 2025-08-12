// ItemPriceManager.cs
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using System.Collections.Generic;
using Content.Shared.Economy; 

namespace Content.Server.Economy
{
    public sealed class ItemPriceManager
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!; 
        public Dictionary<string, int> CurrentPrices { get; private set; } = new();
        
        private readonly Dictionary<string, (int min, int max)> _priceCategories = new()
        {
            ["food_cheap"] = (2, 5),
            ["food_medium"] = (5, 10),
            ["drink"] = (5, 10),
            ["tool_basic"] = (25, 50),
            ["medical"] = (40, 60),
            ["clothing"] = (5, 10)
        };

        public void RecalculatePricesForRound()
        {
            CurrentPrices.Clear();

            foreach (var proto in _prototype.EnumeratePrototypes<EntityPrototype>())
            {
                
                if (!proto.TryGetComponent(out ItemPriceComponent? priceComp, _componentFactory))
                    continue;

                if (!_priceCategories.TryGetValue(priceComp.PriceCategory, out var range))
                {
                    range = (priceComp.FallbackPrice, priceComp.FallbackPrice);
                }

                int newPrice = _random.Next(range.min, range.max + 1);
                CurrentPrices[proto.ID] = newPrice;
            }
        }

        public int? GetPrice(string prototypeId)
        {
            return CurrentPrices.TryGetValue(prototypeId, out var price) ? price : null;
        }
    }
}