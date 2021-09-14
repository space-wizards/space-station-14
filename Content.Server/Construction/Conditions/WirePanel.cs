using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Server.WireHacking;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class WirePanel : IGraphCondition
    {
        [DataField("open")] public bool Open { get; private set; } = true;

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out WiresComponent? wires)) return false;

            return wires.IsPanelOpen == Open;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out WiresComponent? wires)) return false;

            switch (Open)
            {
                case true when !wires.IsPanelOpen:
                    message.AddMarkup(Loc.GetString("construction-condition-wire-panel-open") + "\n");
                    return true;
                case false when wires.IsPanelOpen:
                    message.AddMarkup(Loc.GetString("construction-condition-wire-panel-close") + "\n");
                    return true;
            }

            return false;
        }
    }
}
