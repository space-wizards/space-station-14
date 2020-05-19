using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Client.GameObjects.Components.Weapons.Ranged
{
    [RegisterComponent]
    public sealed class ClientRangedWeaponComponent : SharedRangedWeaponComponent
    {
        public void SyncFirePos(GridCoordinates worldPos)
        {
            SendNetworkMessage(new SyncFirePosMessage(worldPos));
        }
    }
}
