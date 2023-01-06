namespace Content.Server.PlasmaCutter.Components
{
    [RegisterComponent]
    public sealed class PlasmaCutterAmmoComponent : Component
    {
        //How much ammo we refill
        [ViewVariables(VVAccess.ReadWrite)][DataField("refillAmmo")] public int RefillAmmo = 10;
    }
}
