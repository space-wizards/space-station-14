using Content.Server.Construction.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class PartAssemblyComplete : IGraphCondition
    {
        /// <summary>
        /// A valid ID on <see cref="PartAssemblyComponent"/>'s dictionary of strings to part lists.
        /// </summary>
        [DataField("assemblyId")]
        public string AssemblyId = string.Empty;

        /// <summary>
        /// A localization string used for
        /// </summary>
        [DataField("guideString")]
        public string GuideString = "construction-guide-condition-part-assembly";

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            return entityManager.System<PartAssemblySystem>().IsAssemblyFinished(uid, AssemblyId);
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;
            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.System<PartAssemblySystem>().IsAssemblyFinished(entity, AssemblyId))
            {
                args.PushMarkup(Loc.GetString(GuideString));
                return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry
            {
                Localization = GuideString,
            };
        }
    }
}
