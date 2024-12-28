using Robust.Shared.Network;

namespace Content.Shared.Starlight.CryoTeleportation;

[RegisterComponent]
public sealed partial class TargetCryoTeleportationComponent : Component
{
    [DataField]
    public EntityUid? Station;

    [DataField]
    public TimeSpan? ExitTime;
    
    [DataField]
    public NetUserId? UserId;
}