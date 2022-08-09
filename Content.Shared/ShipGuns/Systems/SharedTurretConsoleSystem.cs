using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.ShipGuns.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.ShipGuns.Systems;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedTurretConsoleSystem : EntitySystem
{
    [Dependency] protected readonly ActionBlockerSystem _actionBlockerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GunnerComponent, UpdateCanMoveEvent>(HandleMovementBlock);
        SubscribeLocalEvent<GunnerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<GunnerComponent, ComponentShutdown>(HandleGunnerShutdown);
    }

    [Serializable, NetSerializable]
    protected sealed class GunnerComponentState : ComponentState
    {
        public EntityUid? Console { get; }

        public GunnerComponentState(EntityUid? uid)
        {
            Console = uid;
        }
    }

    protected virtual void HandleGunnerShutdown(EntityUid uid, GunnerComponent component, ComponentShutdown args)
    {
        _actionBlockerSystem.UpdateCanMove(uid);
    }

    private void OnStartup(EntityUid uid, GunnerComponent component, ComponentStartup args)
    {
        _actionBlockerSystem.UpdateCanMove(uid);
    }

    private void HandleMovementBlock(EntityUid uid, GunnerComponent component, UpdateCanMoveEvent args)
    {
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (component.Console == null)
            return;

        args.Cancel();
    }
}
