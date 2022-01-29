using System.Collections.Generic;
using Content.Server.Storage.Components;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class StorageOpen : IGraphCondition
    {
        [DataField("open")]
        public bool Open { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out EntityStorageComponent? entityStorageComponent))
                return false;

            return entityStorageComponent.Open == Open;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var entity = args.Examined;

            if (!entMan.TryGetComponent(entity, out EntityStorageComponent? entityStorage)) return false;

            var metaData = entMan.GetComponent<MetaDataComponent>(entity);

            if (entityStorage.Open != Open)
            {
                if (Open == true)
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-open", ("entityName", metaData.EntityName)) + "\n");
                else
                    args.PushMarkup(Loc.GetString("construction-examine-condition-door-closed", ("entityName", metaData.EntityName)) + "\n");
                return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Open
                    ? "construction-guide-condition-door-open"
                    : "construction-guide-condition-door-closed",
            };
        }
    }
}
