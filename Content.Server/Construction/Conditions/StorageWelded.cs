using Content.Server.Storage.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class StorageWelded : IGraphCondition
    {
        [DataField("welded")]
        public bool Welded { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out EntityStorageComponent? entityStorageComponent))
                return false;

            return entityStorageComponent.IsWeldedShut == Welded;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var entity = args.Examined;

            if (!entMan.TryGetComponent(entity, out EntityStorageComponent? entityStorage)) return false;

            var metaData = entMan.GetComponent<MetaDataComponent>(entity);

            if (entityStorage.IsWeldedShut != Welded)
            {
                if (Welded)
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-weld", ("entityName", metaData.EntityName)) + "\n");
                else
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-unweld", ("entityName", metaData.EntityName)) + "\n");
                return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Welded
                    ? "construction-guide-condition-door-weld"
                    : "construction-guide-condition-door-unweld",
            };
        }
    }
}
