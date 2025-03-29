namespace Content.Server.GameTicking.Rules;

[RegisterComponent]
public sealed partial class GeneralScurretMayhemComponent : Component
{
    [DataField]
    public float ChanceOfMayhem = 0.1f; // About a 85% chance of happening in a round with 6 tots and 3 ghost'd scurrets

    public EntityUid? Scurret;
}
