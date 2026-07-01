namespace Content.Server.Electrocution;

[RegisterComponent]
public sealed partial class RandomInsulationComponent : Component
{
    [DataField]
    public float[] List = { 0f };
}
