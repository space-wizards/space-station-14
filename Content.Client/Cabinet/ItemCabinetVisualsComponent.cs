namespace Content.Client.Cabinet;

[RegisterComponent]
public sealed class ItemCabinetVisualsComponent : Component
{
    [DataField("openState", required: true)]
    public string OpenState = default!;

    [DataField("closedState", required: true)]
    public string ClosedState = default!;
}
