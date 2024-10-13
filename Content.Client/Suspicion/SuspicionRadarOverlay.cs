using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;

namespace Content.Client.Suspicion;

public sealed class SuspicionRadarOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly SharedMapSystem _mapSystem;
    private readonly SuspicionRadarOverlaySystem _suspicionRadarOverlaySystem;
    private readonly TransformSystem _transformSystem;

    private readonly Font _font;

    private const float MinSize = 0.5f;
    private const float MaxSize = 1.5f;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public SuspicionRadarOverlay()
    {
        IoCManager.InjectDependencies(this);

        _mapSystem = _entityManager.System<SharedMapSystem>();
        _suspicionRadarOverlaySystem = _entityManager.System<SuspicionRadarOverlaySystem>();
        _transformSystem = _entityManager.System<TransformSystem>();

        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 8);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.ScreenHandle;

        if (_player.LocalEntity is not { } localEntity)
            return;

        if (args.ViewportControl == null)
            return;

        var localPlayerPosition = _transformSystem.GetWorldPosition(localEntity);
        var bounds = args.ViewportBounds;

        foreach (var radarInfo in _suspicionRadarOverlaySystem.RadarInfos)
        {
            var distance = Vector2.Distance(radarInfo.Position, localPlayerPosition);
            var screenPosition = args.ViewportControl.WorldToScreen(radarInfo.Position);

            // Size of the radar blip is based on the distance from the player. The closer the player is, the smaller the blip.
            var radius = Math.Clamp((int)(MaxSize - distance), MinSize, MaxSize);

            // We clamp the radar blips to the screen bounds so you always see them.

            if (screenPosition.X > bounds.Right)
            {
                screenPosition.X = bounds.Right;
            }
            else if (screenPosition.X < bounds.Left)
            {
                screenPosition.X = bounds.Left;
            }

            if (screenPosition.Y > bounds.Bottom)
            {
                screenPosition.Y = bounds.Bottom;
            }
            else if (screenPosition.Y < bounds.Top)
            {
                screenPosition.Y = bounds.Top;
            }

            handle.DrawCircle(screenPosition, radius, radarInfo.Color, false);
        }
    }
}
