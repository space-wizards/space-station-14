namespace Content.Client.Revenant;

[RegisterComponent]
public sealed class RevenantVisualsComponent : Component
{
    [DataField("state")]
    public string State = "idle";
    [DataField("corporealState")]
    public string CorporealState = "active";
    [DataField("stunnedState")]
    public string StunnedState = "stunned";
    [DataField("harvestingState")]
    public string HarvestingState = "harvesting";
}
