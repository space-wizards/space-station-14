namespace Content.Server.HotPotato;

[RegisterComponent]
public sealed class HotPotatoComponent : Component
{
    [DataField("canTransfer")]
    public bool CanTransfer = true;
}
