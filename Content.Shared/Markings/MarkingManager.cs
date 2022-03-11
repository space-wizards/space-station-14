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

            // _index.Sort();
        }

        public IReadOnlyList<MarkingPrototype> Markings() => _index;
        public IReadOnlyDictionary<MarkingCategories, List<MarkingPrototype>> CategorizedMarkings() => _markingDict;

        // the most DEVIOUS lick
        // mostly because i seriously don't like the whole out thing, but whatever
        // TODO: O(n) to O(log n)
        public bool IsValidMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingResult)
        {
            foreach (var markingPrototype in _index)
            {
                if (marking.MarkingId == markingPrototype.ID)
                {
                    if (markingPrototype.MarkingPartNames.Count
                            == markingPrototype.Sprites.Count)
                    {
                        /*
                        if (marking.MarkingColors.Count != markingPrototype.Sprites.Count)
                        {
                            List<Color> colors = new();
                            for (int i = 0; i < markingPrototype.Sprites.Count; i++)
                            {
                                colors.Add(Color.White);
                            }
                            marking = new Marking(marking.MarkingId, colors);
                        }
                        */

                        markingResult = markingPrototype;
                        return true;
                    }
                }
            }

            Logger.DebugS("AnthroSystem", $"An error occurred while validing a marking. Marking: {marking}");
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
