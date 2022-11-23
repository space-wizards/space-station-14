namespace Content.Shared.Mech.Equipment.Components;

[RegisterComponent]
public sealed class MechGrabberComponent : Component
{
    [DataField("energyPerGrab")]
    public float EnergyPerGrab = -5;
}
