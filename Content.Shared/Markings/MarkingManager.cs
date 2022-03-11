using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Shared.Markings
{
    public sealed class MarkingManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly List<MarkingPrototype> _index = new();
        private readonly Dictionary<MarkingCategories, List<MarkingPrototype>> _markingDict = new();

        public void Initialize()
        {
            _prototypeManager.PrototypesReloaded += OnPrototypeReload;

            foreach (var category in Enum.GetValues<MarkingCategories>())
                _markingDict.Add(category, new List<MarkingPrototype>());

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<MarkingPrototype>())
            {
                _index.Add(prototype);
                _markingDict[prototype.MarkingCategory].Add(prototype);
            }
        }

        public IReadOnlyList<MarkingPrototype> Markings() => _index;
        public IReadOnlyDictionary<MarkingCategories, List<MarkingPrototype>> CategorizedMarkings() => _markingDict;

        public bool IsValidMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingResult)
        {
            foreach (var markingPrototype in _index)
            {
                if (marking.MarkingId == markingPrototype.ID)
                {
                    if (markingPrototype.MarkingPartNames.Count
                            == markingPrototype.Sprites.Count)
                    {
                        markingResult = markingPrototype;
                        return true;
                    }
                }
            }

            Logger.DebugS("Markings", $"An error occurred while validing a marking. Marking: {marking}");
            markingResult = null;
            return false;
        }

        private void OnPrototypeReload(PrototypesReloadedEventArgs args)
        {
            if(!args.ByType.TryGetValue(typeof(MarkingPrototype), out var set))
                return;


            _index.RemoveAll(i => set.Modified.ContainsKey(i.ID));

            foreach (var prototype in set.Modified.Values)
            {
                var markingPrototype = (MarkingPrototype) prototype;
                _index.Add(markingPrototype);
            }
        }
    }
}
