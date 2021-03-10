#nullable enable
using Content.Server.GameObjects.Components.Doors;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using System.Threading.Tasks;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     A condition that requires the airlock to have bolts up.
    ///     Returns true if the entity doesn't have an airlock component.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public class AirlockBolted : IEdgeCondition
    {
        [DataField("value")] public bool Value { get; private set; } = true;

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out AirlockComponent? airlock)) return true;

            return airlock.BoltsDown == Value;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out AirlockComponent? airlock)) return false;

            switch (Value)
            {
                case true when !airlock.BoltsDown:
                    message.AddMarkup(Loc.GetString("construction-condition-airlock-bolt", ("entityName", entity.Name)));
                    return true;
                case false when airlock.BoltsDown:
                    message.AddMarkup(Loc.GetString("construction-condition-airlock-unbolt", ("entityName", entity.Name)));
                    return true;
            }

            return false;
        }
    }
}
