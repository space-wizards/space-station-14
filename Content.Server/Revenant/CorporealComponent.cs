namespace Content.Server.Revenant;

[RegisterComponent]
public sealed class CorporealComponent : Component
{
    [ViewVariables]
    public float MovementSpeedDebuff = 0.66f;
}
