using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared.Markings
{
    public sealed class MarkingManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly List<MarkingPrototype> _index = new();
        private readonly Dictionary<MarkingCategories, List<MarkingPrototype>> _markingDict = new();
        private readonly Dictionary<string, MarkingPrototype> _markings = new();

        public void Initialize()
        {
            _prototypeManager.PrototypesReloaded += OnPrototypeReload;

            foreach (var category in Enum.GetValues<MarkingCategories>())
                _markingDict.Add(category, new List<MarkingPrototype>());

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<MarkingPrototype>())
            {
                _index.Add(prototype);
                _markingDict[prototype.MarkingCategory].Add(prototype);
                _markings.Add(prototype.ID, prototype);
            }
        }

        public IReadOnlyDictionary<string, MarkingPrototype> Markings() => _markings;
        public IReadOnlyDictionary<MarkingCategories, List<MarkingPrototype>> CategorizedMarkings() => _markingDict;

        public IReadOnlyDictionary<MarkingCategories, List<MarkingPrototype>> MarkingsBySpecies(string species)
        {
            var result = new Dictionary<MarkingCategories, List<MarkingPrototype>>(_markingDict);

            foreach (var list in result.Values)
            {
                list.RemoveAll(marking => marking.SpeciesRestrictions != null && marking.SpeciesRestrictions.Contains(species));
            }

            return result;
        }

        public bool IsValidMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingResult)
        {
            return _markings.TryGetValue(marking.MarkingId, out markingResult);
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
