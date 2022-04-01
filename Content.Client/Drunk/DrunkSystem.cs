using Content.Shared.Drunk;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Drunk;

public sealed class DrunkSystem : SharedDrunkSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrunkComponent, ComponentInit>(OnDrunkInit);
        SubscribeLocalEvent<DrunkComponent, ComponentShutdown>(OnDrunkShutdown);
    }

    private void OnDrunkInit(EntityUid uid, DrunkComponent component, ComponentInit args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid && !_overlay.HasOverlay<DrunkOverlay>())
            _overlay.AddOverlay(new DrunkOverlay());
    }

    private void OnDrunkShutdown(EntityUid uid, DrunkComponent component, ComponentShutdown args)
    {
        if (_player.LocalPlayer?.ControlledEntity == uid && _overlay.HasOverlay<DrunkOverlay>())
            _overlay.RemoveOverlay<DrunkOverlay>();
    }
}
