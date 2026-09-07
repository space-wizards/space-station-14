using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Systems;
using Content.Shared.Timing.Systems;

namespace Content.Client.Cargo.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class ClientPriceGunSystem : SharedPriceGunSystem
{
    [Dependency] private UseDelaySystem _useDelay = default!;

    protected override bool GetPriceOrBounty(Entity<PriceGunComponent> entity, EntityUid target, EntityUid user)
    {
        // It feels worse if the cooldown is predicted but the popup isn't! So only do the cooldown reset on the server.
        return _useDelay.IsDelayed(entity.Owner);
    }
}
