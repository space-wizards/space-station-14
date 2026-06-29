using Content.Shared.Item;
using Content.Shared.Throwing;
using Robust.Client.Physics;
using Robust.Client.Player;

namespace Content.Client.Throwing;

public sealed partial class ThrownItemSystem : SharedThrownItemSystem
{
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThrownItemComponent, UpdateIsPredictedEvent>(OnPredictedUpdate);
        SubscribeLocalEvent<ThrownItemComponent, StopThrowEvent>(OnStopThrow);
    }

    private void OnPredictedUpdate(Entity<ThrownItemComponent> thrown, ref UpdateIsPredictedEvent args)
    {
        if (!thrown.Comp.Landed && thrown.Comp.Running && _playerManager.LocalEntity == thrown.Comp.Thrower)
            args.IsPredicted = true;
    }

    private void OnStopThrow(Entity<ThrownItemComponent> thrown, ref StopThrowEvent args)
    {
    }
}
