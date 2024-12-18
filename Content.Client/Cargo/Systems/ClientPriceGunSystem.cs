using Content.Shared.Timing;
using Content.Shared.Cargo.Systems;

namespace Content.Client.Cargo.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class ClientPriceGunSystem : SharedPriceGunSystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    protected override bool GetPriceOrBounty(EntityUid priceGunUid, EntityUid target, EntityUid user)
    {
        if (!TryComp(priceGunUid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((priceGunUid, useDelay)))
            return false;

        // It feels worse if the cooldown is predicted but the popup isn't! So only do the cooldown reset on the server.
        return true;
    }
}
