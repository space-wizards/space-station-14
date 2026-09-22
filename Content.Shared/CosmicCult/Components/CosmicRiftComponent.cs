namespace Content.Shared.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicRiftComponent : Component
{
    [DataField] public bool Used;

    [DataField] public bool Occupied;

    [DataField] public TimeSpan AbsorbTime = TimeSpan.FromSeconds(25);
}
