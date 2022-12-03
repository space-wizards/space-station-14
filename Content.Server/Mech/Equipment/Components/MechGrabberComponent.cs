using Robust.Shared.Containers;

namespace Content.Server.Mech.Equipment.Components;

[RegisterComponent]
public sealed class MechGrabberComponent : Component
{
    [DataField("energyPerGrab")]
    public float EnergyPerGrab = -20;

    [DataField("maxContents")]
    public int MaxContents = 15;

    public Container ItemContainer = default!;
}
