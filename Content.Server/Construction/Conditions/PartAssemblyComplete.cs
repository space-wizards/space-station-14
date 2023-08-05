using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class PartAssemblyComplete : IGraphCondition
    {
        [DataField("assemblyId")]
        public string AssemblyId = string.Empty;

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
                args.PushMarkup(Loc.GetString("construction-guide-condition-part-assembly"));
                return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry
            {
                Localization = "construction-guide-condition-part-assembly",
            };
        }
    }
}
