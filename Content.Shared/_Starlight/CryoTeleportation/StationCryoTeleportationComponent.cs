using Robust.Shared.Audio;

namespace Content.Shared.Starlight.CryoTeleportation;

[RegisterComponent]
public sealed partial class StationCryoTeleportationComponent : Component
{
    [DataField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(300); //5 Minutes

    [DataField]
    public string PortalPrototype = "CryoPortal";

    [DataField]
    public SoundSpecifier TransferSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}