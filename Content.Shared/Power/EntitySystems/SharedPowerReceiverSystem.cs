namespace Content.Shared.Power.EntitySystems;

public abstract class SharedPowerReceiverSystem : EntitySystem
{
    public abstract bool IsPoweredShared(EntityUid uid);
}
