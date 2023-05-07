using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.DoAfter;

/// <summary>
/// Handles events that need to happen after a certain amount of time where the event could be cancelled by factors
/// such as moving.
/// </summary>
public sealed class DoAfterSystem : SharedDoAfterSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new DoAfterOverlay(EntityManager, _prototype, GameTiming));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<DoAfterOverlay>();
    }

    public override void Update(float frameTime)
    {
        // Currently this only predicts do afters initiated by the player.

        // TODO maybe predict do-afters if the local player is the target of some other players do-after? Specifically
        // ones that depend on the target not moving, because the cancellation of those do afters should be readily
        // predictable by clients.

        var playerEntity = _player.LocalPlayer?.ControlledEntity;

        if (!TryComp(playerEntity, out ActiveDoAfterComponent? active))
            return;

        if (_metadata.EntityPaused(playerEntity.Value))
            return;

        var time = GameTiming.CurTime;
        var comp = Comp<DoAfterComponent>(playerEntity.Value);
        var xformQuery = GetEntityQuery<TransformComponent>();
        var handsQuery = GetEntityQuery<HandsComponent>();
        Update(playerEntity.Value, active, comp, time, xformQuery, handsQuery);
    }
}
