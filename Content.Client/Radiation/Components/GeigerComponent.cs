using Content.Shared.Radiation.Components;

namespace Content.Client.Radiation.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedGeigerComponent))]
public sealed class GeigerComponent : SharedGeigerComponent
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("showControl")]
    public bool ShowControl;

    public bool UiUpdateNeeded;
}
