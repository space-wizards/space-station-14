using Content.Client.Construction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Shared.Prototypes;

namespace Content.Client.Placement;

/// <summary>
/// Manages the lifetime of <see cref="PlacementDirectionIndicatorOverlay"/>.
/// </summary>
public sealed class PlacementDirectionIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ConstructionSystem _construction = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new PlacementDirectionIndicatorOverlay(EntityManager, _placement, _proto, _sprite, _xform, _construction));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<PlacementDirectionIndicatorOverlay>();
    }
}
