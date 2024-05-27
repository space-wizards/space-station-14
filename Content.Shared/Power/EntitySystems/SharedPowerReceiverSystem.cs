using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerReceiverSystem : EntitySystem
{
    private EntityQuery<SharedApcPowerReceiverComponent> _poweredQuery;

    public override void Initialize()
    {
        base.Initialize();

        _poweredQuery = GetEntityQuery<SharedApcPowerReceiverComponent>();
    }

    /// <summary>
    /// If this takes power, it returns whether it has power.
    /// Otherwise, it returns 'true' because if something doesn't take power
    /// it's effectively always powered.
    /// </summary>
    /// <returns>True when entity has no ApcPowerReceiverComponent or is Powered. False when not.</returns>
    public bool IsPowered(EntityUid uid, SharedApcPowerReceiverComponent? receiver = null)
    {
        return !_poweredQuery.Resolve(uid, ref receiver, false) || receiver.Powered;
    }
}
