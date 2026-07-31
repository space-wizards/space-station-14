// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Teleportation.Components;

[RegisterComponent]
public sealed partial class PortalProjectileComponent : Component
{
    [ViewVariables]
    public EntityUid? Destination;

    [DataField]
    public EntProtoId NearPortal = "PortalGunNear";

    [DataField]
    public EntProtoId FarPortal = "PortalGunFar";

    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(10);
}