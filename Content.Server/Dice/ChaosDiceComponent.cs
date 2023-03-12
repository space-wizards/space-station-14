namespace Content.Server.Dice;

[RegisterComponent]
public sealed class ChaosDiceComponent : Component
{
    [DataField("cooldown"), ViewVariables(VVAccess.ReadWrite)]
    public float Cooldown = 3;
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan lastActivated;
}
