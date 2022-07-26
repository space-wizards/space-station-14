namespace Content.Client.Conveyor.Components;

[RegisterComponent]
public sealed class TwoWayLeverVisualsComponent : Component
{
    [DataField("state_forward")]
    public string? StateForward;

    [DataField("state_off")]
    public string? StateOff;

    [DataField("state_reversed")]
    public string? StateReversed;
}
