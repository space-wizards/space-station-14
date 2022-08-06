namespace Content.Server.RCD.Components
{
    [RegisterComponent]
    public sealed class RCDAmmoComponent : Component
    {
        //How much ammo we refill
        [ViewVariables(VVAccess.ReadWrite)] [DataField("refillAmmo")] public int RefillAmmo = 5;
    }
}
