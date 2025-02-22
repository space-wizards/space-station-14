namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class CosmicVictoryConditionComponent : Component
{
    [DataField] public int Victory = 0;
}
