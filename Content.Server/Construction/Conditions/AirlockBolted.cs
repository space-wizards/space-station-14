using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using System.Threading.Tasks;
using Content.Server.Doors.Components;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class AirlockBolted : IGraphCondition
    {
        [DataField("value")]
        public bool Value { get; private set; } = true;

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out AirlockComponent? airlock)) return true;

            return airlock.BoltsDown == Value;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out AirlockComponent? airlock)) return false;

            if (airlock.BoltsDown != Value)
            {
                if (Value == true)
                    message.AddMarkup(Loc.GetString("construction-condition-airlock-bolt", ("entityName", entity.Name)) + "\n");
                else
                    message.AddMarkup(Loc.GetString("construction-condition-airlock-unbolt", ("entityName", entity.Name)) + "\n");
                return true;
            }

            return false;
        }
    }
}
