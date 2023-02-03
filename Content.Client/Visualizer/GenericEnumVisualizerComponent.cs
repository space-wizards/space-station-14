namespace Content.Client.Visualizer;

[RegisterComponent]
[Access(typeof(GenericEnumVisualizerSystem))]
public sealed class GenericEnumVisualizerComponent : Component
{
    public Enum Key { get; set; } = default!;

    public Dictionary<object, string> States { get; set; } = default!;

    [DataField("layer")]
    public int Layer { get; set; } = 0;

    [DataField("key", readOnly: true, required: true)]
    public string KeyRaw = default!;

    [DataField("states", readOnly: true, required: true)]
    public Dictionary<string, string> StatesRaw { get; set; } = default!;
}
