using Content.Client.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Effects;

public sealed partial class ScreechShockWaveSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    public override void Initialize()
    {
    }
}
