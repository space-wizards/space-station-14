using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.RCD.Components
{
    [RegisterComponent]
    public class RCDAmmoComponent : Component
    {
        //How much ammo we refill
        [ViewVariables(VVAccess.ReadWrite)] [DataField("refillAmmo")] public int RefillAmmo = 5;
    }
}
