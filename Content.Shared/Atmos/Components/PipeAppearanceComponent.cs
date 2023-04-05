namespace Content.Shared.Atmos.Components;

[RegisterComponent]
public sealed class PipeAppearanceComponent : Component
{
    [DataField("rsi")]
    public string RsiPath = "Structures/Piping/Atmospherics/pipe.rsi";

    [DataField("baseState")]
    public string State = "pipeConnector";
}
