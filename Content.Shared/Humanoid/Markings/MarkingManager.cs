using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Markings
{
    public sealed class MarkingManager
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly List<MarkingPrototype> _index = new();
        private readonly Dictionary<MarkingCategories, Dictionary<string, MarkingPrototype>> _markingDict = new();
        private readonly Dictionary<string, MarkingPrototype> _markings = new();

        public void Initialize()
        {
            _prototypeManager.PrototypesReloaded += OnPrototypeReload;

            foreach (var category in Enum.GetValues<MarkingCategories>())
            {
                _markingDict.Add(category, new Dictionary<string, MarkingPrototype>());
            }

            foreach (var prototype in _prototypeManager.EnumeratePrototypes<MarkingPrototype>())
            {
                _index.Add(prototype);
                _markingDict[prototype.MarkingCategory].Add(prototype.ID, prototype);
                _markings.Add(prototype.ID, prototype);
            }
        }

        public IReadOnlyDictionary<string, MarkingPrototype> Markings => _markings;
        public IReadOnlyDictionary<MarkingCategories, Dictionary<string, MarkingPrototype>> CategorizedMarkings => _markingDict;

        public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByCategory(MarkingCategories category)
        {
            // all marking categories are guaranteed to have a dict entry
            return _markingDict[category];
        }

        /// <summary>
        ///     Markings by category and species.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="species"></param>
        /// <remarks>
        ///     This is done per category, as enumerating over every single marking by species isn't useful.
        ///     Please make a pull request if you find a use case for that behavior.
        /// </remarks>
        /// <returns></returns>
        public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByCategoryAndSpecies(MarkingCategories category,
            string species)
        {
            var speciesProto = _prototypeManager.Index<SpeciesPrototype>(species);
            var onlyWhitelisted = _prototypeManager.Index<MarkingPointsPrototype>(speciesProto.MarkingPoints).OnlyWhitelisted;
            var res = new Dictionary<string, MarkingPrototype>();

            foreach (var (key, marking) in MarkingsByCategory(category))
            {
                if (onlyWhitelisted && marking.SpeciesRestrictions == null)
                {
                    continue;
                }

                if (marking.SpeciesRestrictions != null && !marking.SpeciesRestrictions.Contains(species))
                {
                    continue;
                }
                res.Add(key, marking);
            }

            return res;
        }

        /// <summary>
        ///     Markings by category and sex.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="sex"></param>
        /// <remarks>
        ///     This is done per category, as enumerating over every single marking by species isn't useful.
        ///     Please make a pull request if you find a use case for that behavior.
        /// </remarks>
        /// <returns></returns>
        public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByCategoryAndSex(MarkingCategories category,
            Sex sex)
        {
            var res = new Dictionary<string, MarkingPrototype>();

            foreach (var (key, marking) in MarkingsByCategory(category))
            {
                if (marking.SexRestriction != null && marking.SexRestriction != sex)
                {
                    continue;
                }

                res.Add(key, marking);
            }

            return res;
        }

        /// <summary>
        ///     Markings by category, species and sex.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="species"></param>
        /// <param name="sex"></param>
        /// <remarks>
        ///     This is done per category, as enumerating over every single marking by species isn't useful.
        ///     Please make a pull request if you find a use case for that behavior.
        /// </remarks>
        /// <returns></returns>
        public IReadOnlyDictionary<string, MarkingPrototype> MarkingsByCategoryAndSpeciesAndSex(MarkingCategories category,
            string species, Sex sex)
        {
            var speciesProto = _prototypeManager.Index<SpeciesPrototype>(species);
            var onlyWhitelisted = _prototypeManager.Index<MarkingPointsPrototype>(speciesProto.MarkingPoints).OnlyWhitelisted;
            var res = new Dictionary<string, MarkingPrototype>();

            foreach (var (key, marking) in MarkingsByCategory(category))
            {
                if (onlyWhitelisted && marking.SpeciesRestrictions == null)
                {
                    continue;
                }

                if (marking.SpeciesRestrictions != null && !marking.SpeciesRestrictions.Contains(species))
                {
                    continue;
                }

                if (marking.SexRestriction != null && marking.SexRestriction != sex)
                {
                    continue;
                }

                res.Add(key, marking);
            }

            return res;
        }

        public bool TryGetMarking(Marking marking, [NotNullWhen(true)] out MarkingPrototype? markingResult)
        {
            return _markings.TryGetValue(marking.MarkingId, out markingResult);
        }

        /// <summary>
        ///     Check if a marking is valid according to the category, species, and current data this marking has.
        /// </summary>
        /// <param name="marking"></param>
        /// <param name="category"></param>
        /// <param name="species"></param>
        /// <param name="sex"></param>
        /// <returns></returns>
        public bool IsValidMarking(Marking marking, MarkingCategories category, string species, Sex sex)
        {
            if (!TryGetMarking(marking, out var proto))
            {
                return false;
            }

            if (proto.MarkingCategory != category ||
                proto.SpeciesRestrictions != null && !proto.SpeciesRestrictions.Contains(species) ||
                proto.SexRestriction != null && proto.SexRestriction != sex)
            {
                return false;
            }

            if (marking.MarkingColors.Count != proto.Sprites.Count)
            {
                return false;
            }

            return true;
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

        public bool CanBeApplied(string species, Sex sex, Marking marking, IPrototypeManager? prototypeManager = null)
        {
            IoCManager.Resolve(ref prototypeManager);

            var speciesProto = prototypeManager.Index<SpeciesPrototype>(species);
            var onlyWhitelisted = prototypeManager.Index<MarkingPointsPrototype>(speciesProto.MarkingPoints).OnlyWhitelisted;

            if (!TryGetMarking(marking, out var prototype))
            {
                return false;
            }

            if (onlyWhitelisted && prototype.SpeciesRestrictions == null)
            {
                return false;
            }

            if (prototype.SpeciesRestrictions != null
                && !prototype.SpeciesRestrictions.Contains(species))
            {
                return false;
            }

            if (prototype.SexRestriction != null && prototype.SexRestriction != sex)
            {
                return false;
            }

            return true;
        }

        public bool CanBeApplied(string species, Sex sex, MarkingPrototype prototype, IPrototypeManager? prototypeManager = null)
        {
            IoCManager.Resolve(ref prototypeManager);

            var speciesProto = prototypeManager.Index<SpeciesPrototype>(species);
            var onlyWhitelisted = prototypeManager.Index<MarkingPointsPrototype>(speciesProto.MarkingPoints).OnlyWhitelisted;

            if (onlyWhitelisted && prototype.SpeciesRestrictions == null)
            {
                return false;
            }

            if (prototype.SpeciesRestrictions != null &&
                !prototype.SpeciesRestrictions.Contains(species))
            {
                return false;
            }

            if (prototype.SexRestriction != null && prototype.SexRestriction != sex)
            {
                return false;
            }

            return true;
        }

        public bool MustMatchSkin(string species, HumanoidVisualLayers layer, out float alpha, IPrototypeManager? prototypeManager = null)
        {
            IoCManager.Resolve(ref prototypeManager);
            var speciesProto = prototypeManager.Index<SpeciesPrototype>(species);
            if (
                !prototypeManager.TryIndex(speciesProto.SpriteSet, out HumanoidSpeciesBaseSpritesPrototype? baseSprites) ||
                !baseSprites.Sprites.TryGetValue(layer, out var spriteName) ||
                !prototypeManager.TryIndex(spriteName, out HumanoidSpeciesSpriteLayer? sprite) ||
                sprite == null ||
                !sprite.MarkingsMatchSkin
            )
            {
                alpha = 1f;
                return false;
            }

            alpha = sprite.LayerAlpha;
            return true;
        }
    }
}
