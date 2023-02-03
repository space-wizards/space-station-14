namespace Content.Client.Recycling;

[RegisterComponent]
public sealed class RecyclerVisualizerComponent : Component
{
    [DataField("state_on")]
    [Access(typeof(RecyclerVisualizerSystem))]
    public string StateOn = "grinder-o1";

    [DataField("state_off")]
    [Access(typeof(RecyclerVisualizerSystem))]
    public string StateOff = "grinder-o0";
}
