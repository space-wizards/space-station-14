using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Appearance
{
    public sealed class SpriteAccessoryManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<SpriteAccessoryCategory, List<SpriteAccessoryPrototype>> _index = new();

        public void Initialize()
        {
            _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;

            foreach (var category in Enum.GetValues<SpriteAccessoryCategory>())
            {
                _index.Add(category, new List<SpriteAccessoryPrototype>());
            }

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<SpriteAccessoryPrototype>())
            {
                _index[prototype.Category].Add(prototype);
            }
        }

        public IReadOnlyList<SpriteAccessoryPrototype> AccessoriesForCategory(SpriteAccessoryCategory category)
        {
            return _index[category];
        }

        public bool IsValidAccessoryInCategory(string accessory, SpriteAccessoryCategory category)
        {
            return _prototypeManager.TryIndex(accessory, out SpriteAccessoryPrototype? accessoryPrototype)
                   && accessoryPrototype.Category == category;
        }

        private void OnPrototypesReloaded(PrototypesReloadedEventArgs eventArgs)
        {
            if (!eventArgs.ByType.TryGetValue(typeof(SpriteAccessoryPrototype), out var set))
                return;

            foreach (var list in _index.Values)
            {
                list.RemoveAll(a => set.Modified.ContainsKey(a.ID));
            }

            foreach (var prototype in set.Modified.Values)
            {
                var accessoryPrototype = (SpriteAccessoryPrototype) prototype;
                _index[accessoryPrototype.Category].Add(accessoryPrototype);
            }
        }
    }
}
