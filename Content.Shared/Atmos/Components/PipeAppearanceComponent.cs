namespace Content.Client.Atmos.Components;

[RegisterComponent]
public sealed class PipeVisualsComponent : Component
{
    [DataField("rsi")]
    public string RsiPath = "Structures/Piping/Atmospherics/pipe.rsi";

    [DataField("baseState")]
    public string BaseState = "pipeConnector";
}
