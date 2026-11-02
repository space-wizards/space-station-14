using System.Numerics;
using Content.Shared.CF.Combat;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Client.CF.Combat;

/// <summary>
/// Debug label. Draws green "OffGuard" above any creature that is currently off-guard.
/// </summary>
public sealed partial class CFOffGuardLabelSystem : EntitySystem
{
    private const float HeadClearance = 42f;
    private static readonly Color LabelColor = new(70, 220, 90);

    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private Font _font = default!;

    public override void Initialize()
    {
        base.Initialize();
        _font = new VectorFont(_cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Bold.ttf"), 14);
        _overlays.AddOverlay(new CFOffGuardLabelOverlay(this));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay<CFOffGuardLabelOverlay>();
    }

    public void Draw(DrawingHandleScreen handle, OverlayDrawArgs args, float uiScale)
    {
        if (args.ViewportControl == null)
            return;

        var matrix = args.ViewportControl.GetWorldToScreenMatrix();
        var query = EntityQueryEnumerator<CFOffGuardComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var offGuard, out var xform))
        {
            if (!offGuard.Active)
                continue;

            var map = _transform.GetMapCoordinates(uid, xform);
            if (map.MapId != args.MapId || !args.WorldBounds.Contains(map.Position))
                continue;

            var screen = Vector2.Transform(map.Position, matrix);
            screen.Y -= HeadClearance * uiScale;

            const string text = "OffGuard";
            var size = handle.GetDimensions(_font, text, uiScale);
            var position = screen - size / 2f;
            handle.DrawString(_font, position, text, uiScale, LabelColor, new TextOutline(1f, Color.Black));
        }
    }

    public float UiScale()
    {
        var scale = _config.GetCVar(CVars.DisplayUIScale);
        return scale == 0f ? _ui.DefaultUIScale : scale;
    }
}
