using Content.Client.SubFloor;
using Content.Shared.VentCraw;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.VentCraw;

public sealed class VentCrawSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SubFloorHideSystem _subFloorHideSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalPlayer?.ControlledEntity;

        var ventCraslerQuery = GetEntityQuery<VentCrawlerComponent>();

        if (!ventCraslerQuery.TryGetComponent(player, out var playerVentCrawlerComponent))
        {
            _subFloorHideSystem.ShowVentPipe = false;
            return;
        }

        _subFloorHideSystem.ShowVentPipe = playerVentCrawlerComponent.InTube;
    }
}
