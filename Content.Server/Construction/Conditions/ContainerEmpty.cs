using Content.Shared.Conditions.Satisfier;
using Content.Shared.Conditions.UnifiedConditions;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class ContainerEmpty : GraphCondition,IContainerContentsCountCondition
    {

        /// <summary>
        /// The base condition just counts contained entities, this satisfier makes sure the condition is only met, when actually empty.
        /// </summary>
        public ContainerEmpty()
        {
            Satisfier = new WithThreshold()
            {
                Comparison = WithThreshold.Comparator.Equal,
                Threshold = 0
            };
        }

        [DataField("container")]
        public string Container { get; private set; } = string.Empty;

        [DataField("examineText")]
        public string? ExamineText { get; private set; }

        [DataField("guideStep")]
        public string? GuideText { get; private set; }

        [DataField("guideIcon")]
        public SpriteSpecifier? GuideIcon { get; private set; }

        public override bool DoExamine(ExaminedEvent args)
        {
            if (string.IsNullOrEmpty(ExamineText))
                return false;

            var entity = args.Examined;

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.TryGetComponent(entity, out ContainerManagerComponent? containerManager) ||
                !entityManager.System<SharedContainerSystem>().TryGetContainer(entity, Container, out var container, containerManager))
                return false;

            if (container.ContainedEntities.Count == 0)
                return false;

            args.PushMarkup(Loc.GetString(ExamineText));
            return true;
        }
        public override IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            if (string.IsNullOrEmpty(GuideText))
                yield break;

            yield return new ConstructionGuideEntry()
            {
                Localization = GuideText,
                Icon = GuideIcon,
            };
        }
    }
}
