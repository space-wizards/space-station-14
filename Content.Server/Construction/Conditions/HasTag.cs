using Content.Shared.Construction;
using JetBrains.Annotations;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using YamlDotNet.Core.Tokens;
using Content.Shared.Tag;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class HasTag : IGraphCondition
    {
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
