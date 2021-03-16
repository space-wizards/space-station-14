using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Doors;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using static Content.Shared.GameObjects.Components.Doors.SharedDoorComponent;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class DoorWelded : IEdgeCondition
    {
        [DataField("welded")] public bool Welded { get; private set; } = true;

        public async Task<bool> Condition(IEntity entity)
        {
            if (!entity.TryGetComponent(out ServerDoorComponent? doorComponent)) return false;
            return doorComponent.IsWeldedShut == Welded;
        }

        public bool DoExamine(IEntity entity, FormattedMessage message, bool inDetailsRange)
        {
            if (!entity.TryGetComponent(out ServerDoorComponent? doorComponent)) return false;

            if (doorComponent.State == DoorState.Closed && Welded)
            {
                message.AddMarkup(Loc.GetString("First, weld the door.\n"));
                return true;
            }

            if (!doorComponent.IsWeldedShut || Welded) return false;

            message.AddMarkup(Loc.GetString("First, unweld the door.\n"));
            return true;

        }
    }
}
