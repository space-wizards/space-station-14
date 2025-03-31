namespace Content.Server.GameTicking.Rules;

[RegisterComponent]
public sealed partial class GeneralScurretMayhemComponent : Component
{
    [DataField]
    public float ChanceOfMayhem = 0.3f;

    [DataField]
    public bool Handled; // prevent them from being selected multiple times
}
