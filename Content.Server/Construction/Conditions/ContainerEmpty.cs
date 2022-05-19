using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class ContainerEmpty : IGraphCondition
    {
        [DataField("container")]
        public string Container { get; } = string.Empty;

        [DataField("examineText")]
        public string? ExamineText { get; }

        [DataField("guideStep")]
        public string? GuideText { get; }

        [DataField("guideIcon")]
        public SpriteSpecifier? GuideIcon { get; }

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();
            if (!containerSystem.TryGetContainer(uid, Container, out var container))
                return false;

            return container.ContainedEntities.Count == 0;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            if (string.IsNullOrEmpty(ExamineText))
                return false;

            var entity = args.Examined;

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return false;

            if (container.ContainedEntities.Count == 0)
                return false;

            args.PushMarkup(Loc.GetString(ExamineText));
            return true;
        }
        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
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
