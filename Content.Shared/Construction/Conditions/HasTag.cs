using Content.Shared.Examine;
using Content.Shared.Tag;
using JetBrains.Annotations;

namespace Content.Shared.Construction.Conditions
{
    /// <summary>
    ///     This condition checks whether if an entity with the <see cref="TagComponent"/> possesses a specific tag
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class HasTag : IGraphCondition
    {
        /// <summary>
        ///     The tag the entity is being checked for
        /// </summary>
        [DataField("tag")]
        public string Tag { get; private set; }

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TrySystem<TagSystem>(out var tagSystem))
                return false;

            return tagSystem.HasTag(uid, Tag);
        }

        public bool DoExamine(ExaminedEvent args)
        {
            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
            };
        }
    }
}
