using Content.Shared.Radiation.Components;

namespace Content.Client.Radiation.Components;

[RegisterComponent]
public sealed class GeigerComponent : SharedGeigerComponent
{
    public bool UiUpdateNeeded;
}
