using Content.Shared.Movement.Systems;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Zombies;

public abstract partial class SharedZombieSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<PendingZombieComponent, EntityUnpausedEvent>(OnUnpause);
    }

    // Apply a zombie slow.
    private void OnRefreshSpeed(EntityUid uid, ZombieComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        var mod = component.MovementSpeedDebuff;
        args.ModifySpeed(mod, mod);
    }

    public void AddZombieTo(EntityUid uid, ZombieComponent? settings, bool turnNow = false)
    {
        var zombie = EnsureComp<ZombieComponent>(uid);

        // Copy settings from the rule
        if (settings != null)
        {
            _serialization.CopyTo(settings, ref zombie, notNullableOverride:true);
        }

        Dirty(uid, zombie);

        if (turnNow)
        {
            // Time to begin forcing this infected to turn.
            Infect(uid, _random.NextFloat(0.25f, 1.0f) * zombie.InfectionTurnTime, zombie.DeadMinTurnTime);
        }
    }
}
