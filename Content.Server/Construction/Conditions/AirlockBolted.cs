using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using System.Threading.Tasks;
using Content.Server.Doors.Components;
using Content.Shared.Examine;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class AirlockBolted : IGraphCondition
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

            if (!entity.TryGetComponent(out AirlockComponent? airlock)) return false;

            if (airlock.BoltsDown != Value)
            {
                if (Value == true)
                    args.PushMarkup(Loc.GetString("construction-condition-airlock-bolt", ("entityName", entity.Name)) + "\n");
                else
                    args.PushMarkup(Loc.GetString("construction-condition-airlock-unbolt", ("entityName", entity.Name)) + "\n");
                return true;
            }

            return false;
        }
    }
}
