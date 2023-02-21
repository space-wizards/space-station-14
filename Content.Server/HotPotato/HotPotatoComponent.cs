namespace Content.Server.HotPotato;

[RegisterComponent]
public sealed class HotPotatoComponent : Component
{
    [DataField("activated")]
    public bool Activated = false;
    [DataField("canTransfer")]
    public bool CanTransfer = true;
}
