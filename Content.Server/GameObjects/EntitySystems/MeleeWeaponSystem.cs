using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class MeleeWeaponSystem : EntitySystem
    {
        public void SendArc(string arc, GridCoordinates position, Angle angle)
        {
            RaiseNetworkEvent(new MeleeWeaponSystemMessages.PlayWeaponArcMessage(arc, position, angle));
        }
    }
}
