using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.Map;

namespace Content.Client.GameObjects.Components.Movement
{
    public class ClientTeleporterComponent : SharedTeleporterComponent
    {
        public void TryClientTeleport(GridCoordinates worldPos)
        {
            SendNetworkMessage(new TeleportMessage(worldPos));
        }
    }
}
