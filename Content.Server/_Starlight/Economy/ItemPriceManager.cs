using Content.Shared.Economy;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Content.Shared.Destructible.Thresholds;

namespace Content.Server.Economy
{
    public sealed class ItemPriceManager : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypes = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        private readonly Dictionary<string, int> _prototypePrices = new();

        private Dictionary<string, MinMax>? _protoPriceCategoriesCache;
        private IReadOnlyDictionary<string, MinMax> GetPriceCategories()
        {
            if (_protoPriceCategoriesCache != null)
                return _protoPriceCategoriesCache;

            var dict = new Dictionary<string, MinMax>();
            foreach (var proto in _prototypes.EnumeratePrototypes<PriceCategoryPrototype>())
            {
                // Guard against bad data (min>max etc.)
                var min = proto.Min;
                var max = proto.Max;
                if (min <= 0 && max <= 0)
                    continue;
                if (min > max)
                    (min, max) = (max, min);
                dict[proto.ID] = new MinMax(min, max);
            }

            // Only use prototype-defined categories; no hardcoded fallback
            _protoPriceCategoriesCache = dict;
            return _protoPriceCategoriesCache;
        }

        /// <summary>
        /// Clears any cached prices and warms the per-prototype cache by enumerating entity prototypes
        /// that declare an ItemPriceComponent. This avoids scanning live entities and ensures
        /// that all items spawned later in the round get a stable, category-based price.
        /// </summary>
        public void ResetForNewRound()
        {
            _prototypePrices.Clear();
            // Keep category cache; it comes from prototypes and won't change at runtime typically
            WarmCacheFromPrototypes();
        }

        private void WarmCacheFromPrototypes()
        {
            var categories = GetPriceCategories();
            foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<ItemPriceComponent>(out var priceComp, _componentFactory))
                    continue;

                if (_prototypePrices.ContainsKey(proto.ID))
                    continue;

                if (!categories.TryGetValue(priceComp.PriceCategory, out var range))
                    continue;

                var price = _random.Next(range.Min, range.Max + 1);
                _prototypePrices[proto.ID] = price;
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

            var categories = GetPriceCategories();
            if (!categories.TryGetValue(category, out var minMax))
            {
                return null;
            }

            var newPrice = _random.Next(minMax.Min, minMax.Max + 1);
            _prototypePrices[prototypeId] = newPrice;
            return newPrice;
        }
    }
}