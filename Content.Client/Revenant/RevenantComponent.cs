using Content.Shared.Revenant;

namespace Content.Client.Revenant;

[RegisterComponent]
public sealed class RevenantComponent : SharedRevenantComponent
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
