using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Doors;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
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
            //if (!entity.TryGetComponent(out IPhysicsComponent physics)) return false;
            ServerDoorComponent doorComponent2 = null;
            FirelockComponent  doorComponent = null;
            if (!entity.TryGetComponent(out doorComponent) && !entity.TryGetComponent(out doorComponent2)) return false;
            if (doorComponent2 == null)
            {
                return doorComponent.IsWeldedShut;
            }else
            {
                return doorComponent2.IsWeldedShut;
            }
        }

        public void DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            //if (!entity.TryGetComponent(out IPhysicsComponent physics)) return;
            if(!entity.TryGetComponent(out ServerDoorComponent doorComponent)) return;

            if ((doorComponent.State & (int)ServerDoorComponent.DoorState.Closed) != 0)
                message.AddMarkup("First, weld the door.\n");
        }
    }
}
