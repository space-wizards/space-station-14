using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Doors;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    public class DoorWelded : IEdgeCondition
    {
        public bool Welded { get; private set; }
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Welded, "welded", true);
        }

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out ServerDoorComponent doorComponent)) return false;
            return doorComponent.IsWeldedShut == Welded;
        }

        public void DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out ServerDoorComponent doorComponent)) return;

            if (doorComponent.State == ServerDoorComponent.DoorState.Closed && Welded)
                message.AddMarkup(Loc.GetString("First, weld the door.\n"));
            else if (doorComponent.IsWeldedShut && !Welded)
            {
                message.AddMarkup(Loc.GetString("First, unweld the door.\n"));
            }
        }
    }
}
