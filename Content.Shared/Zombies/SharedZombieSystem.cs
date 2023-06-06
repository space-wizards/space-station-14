using Content.Shared.Movement.Systems;

namespace Content.Shared.Zombies;

public abstract class SharedZombieSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnRefreshSpeed(EntityUid uid, ZombieComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var mod = component.Settings.MovementSpeedDebuff;
        args.ModifySpeed(mod, mod);
    }
}
