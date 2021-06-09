using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.CharacterAppearance
{
    public sealed class SpriteAccessoryManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<SpriteAccessoryCategories, List<SpriteAccessoryPrototype>> _index = new();

        public void Initialize()
        {
            _prototypeManager.PrototypesReloaded += OnPrototypesReloaded;

            foreach (var category in Enum.GetValues<SpriteAccessoryCategories>())
            {
                _index.Add(category, new List<SpriteAccessoryPrototype>());
            }

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<SpriteAccessoryPrototype>())
            {
                AddToIndexes(prototype);
            }
        }

        public IReadOnlyList<SpriteAccessoryPrototype> AccessoriesForCategory(SpriteAccessoryCategories categories)
        {
            return _index[categories];
        }

        public bool IsValidAccessoryInCategory(string accessory, SpriteAccessoryCategories categories)
        {
            return _prototypeManager.TryIndex(accessory, out SpriteAccessoryPrototype? accessoryPrototype)
                   && (accessoryPrototype.Categories & categories) != 0;
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
                AddToIndexes(accessoryPrototype);
            }
        }

        private void AddToIndexes(SpriteAccessoryPrototype accessoryPrototype)
        {
            for (var i = 0; i < sizeof(SpriteAccessoryCategories) * 8; i++)
            {
                var flag = (SpriteAccessoryCategories) (1 << i);
                if ((accessoryPrototype.Categories & flag) != 0)
                    _index[flag].Add(accessoryPrototype);
            }
        }
    }
}
