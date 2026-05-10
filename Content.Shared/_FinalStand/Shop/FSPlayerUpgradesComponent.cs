namespace Content.Shared._FinalStand.Shop;

[RegisterComponent]
public sealed partial class FSPlayerUpgradesComponent : Component
{
    [DataField]
    public Dictionary<string, int> Levels = new();
}
