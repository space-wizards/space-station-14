namespace Content.Shared.Plasmaman;

[RegisterComponent]
public sealed partial class PlasmamanOxygenProtectionComponent : Component
{
    [DataField]
    public bool ProtectsHead;

    [DataField]
    public bool ProtectsBody;
}
