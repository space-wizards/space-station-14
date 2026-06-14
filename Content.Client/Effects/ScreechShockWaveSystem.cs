using Content.Client.Overlays;
using Content.Shared.Effects;
using Robust.Client.Graphics;

namespace Content.Client.Effects;

public sealed partial class ScreechShockWaveSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ScreechShockWaveComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<ScreechShockWaveComponent> ent, ref ComponentStartup args)
    {
        if (!_overlayMan.HasOverlay<ScreechShockWaveOverlay>())
        {
            _overlayMan.AddOverlay(new ScreechShockWaveOverlay());
        }
    }
}
