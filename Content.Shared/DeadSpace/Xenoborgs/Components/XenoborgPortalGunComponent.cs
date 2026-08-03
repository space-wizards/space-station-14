// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.Xenoborgs.Components;

/// <summary>
/// Tracks the projectile and shot-relative cooldown of a xenoborg portal gun.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class XenoborgPortalGunComponent : Component
{
    [DataField]
    public TimeSpan UnstunnedCooldown = TimeSpan.FromSeconds(120);

    [DataField]
    public TimeSpan StunnedCooldown = TimeSpan.FromSeconds(45);

    [DataField]
    public TimeSpan MissCooldown = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public EntityUid? PendingProjectile;

    [ViewVariables]
    public TimeSpan ShotTime;

    [ViewVariables]
    public TimeSpan PendingUntil;
}
