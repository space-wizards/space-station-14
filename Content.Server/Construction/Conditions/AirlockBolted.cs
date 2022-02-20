using System.Collections.Generic;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using System.Threading.Tasks;
using Content.Server.Doors.Components;
using Content.Shared.Examine;
using Robust.Shared.IoC;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class AirlockBolted : IGraphCondition
    {
        [DataField("value")]
        public bool Value { get; private set; } = true;

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            if (!entityManager.TryGetComponent(uid, out AirlockComponent? airlock))
                return true;

            return airlock.BoltsDown == Value;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            var entity = args.Examined;

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent(entity, out AirlockComponent? airlock)) return false;

            if (airlock.BoltsDown != Value)
            {
                if (Value == true)
                    args.PushMarkup(Loc.GetString("construction-examine-condition-airlock-bolt", ("entityName", entMan.GetComponent<MetaDataComponent>(entity).EntityName)) + "\n");
                else
                    args.PushMarkup(Loc.GetString("construction-examine-condition-airlock-unbolt", ("entityName", entMan.GetComponent<MetaDataComponent>(entity).EntityName)) + "\n");
                return true;
            }

            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield return new ConstructionGuideEntry()
            {
                Localization = Value ? "construction-step-condition-airlock-bolt" : "construction-step-condition-airlock-unbolt"
            };
        }
    }
}
