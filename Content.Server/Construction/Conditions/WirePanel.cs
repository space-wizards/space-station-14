using System.Threading.Tasks;
using Content.Server.GameObjects.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class WirePanel : IEdgeCondition
    {
        public bool Open { get; private set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Open, "open", true);
        }

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out WiresComponent wires)) return false;

            return wires.IsPanelOpen == Open;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out WiresComponent wires)) return false;

            switch (Open)
            {
                case true when !wires.IsPanelOpen:
                    message.AddMarkup(Loc.GetString("First, open the maintenance panel.\n"));
                    return true;
                case false when wires.IsPanelOpen:
                    message.AddMarkup(Loc.GetString("First, close the maintenance panel.\n"));
                    return true;
            }

            return false;
        }
    }
}
