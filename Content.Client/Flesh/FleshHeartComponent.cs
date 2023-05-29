using Content.Shared.Flesh;

namespace Content.Client.Flesh;

[RegisterComponent]
public sealed class FleshHeartComponent : Component
{
    [DataField("finalState")]
    public string? FinalState = "underpowered";
}
