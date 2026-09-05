namespace Content.Server.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicRiftComponent : Component
{
    [DataField] public bool Used;

    [DataField] public bool Occupied;

    [DataField] public TimeSpan PurgeTime = TimeSpan.FromSeconds(35);

    [DataField] public TimeSpan AbsorbTime = TimeSpan.FromSeconds(25);
}
