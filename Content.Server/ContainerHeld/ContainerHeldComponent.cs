namespace Content.Server.ContainerHeld;

[RegisterComponent]
public sealed partial class ContainerHeldComponent: Component
{
    [DataField("threshold")]
    public int Threshold = 1;
}
