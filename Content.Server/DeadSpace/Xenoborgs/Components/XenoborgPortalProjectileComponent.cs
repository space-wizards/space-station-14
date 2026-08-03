// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Xenoborgs.Components;

/// <summary>
/// Links a xenoborg portal projectile back to the gun which fired it.
/// </summary>
[RegisterComponent]
public sealed partial class XenoborgPortalProjectileComponent : Component
{
    [ViewVariables]
    public EntityUid? Gun;
}
