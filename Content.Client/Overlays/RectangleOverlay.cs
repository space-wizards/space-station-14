using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.Overlays;

/// <summary>
/// Overlays a list of gradient rectangles, centered on the user's screen.
/// </summary>
public sealed partial class RectangleOverlay : Overlay
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public readonly List<(ShaderInstance, RectangleShaderValues)> RectangleShaders = new();

    public RectangleOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        // Zoom is required to ensure it stays consistent in-world
        if (!_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var eye))
            return;

        foreach (var (shader, values) in RectangleShaders)
        {
            var handle = args.WorldHandle;

            shader.SetParameter("zoom", eye.Zoom.X);
            shader.SetParameter("color", values.RectColor);
            shader.SetParameter("outerRectangleWidth", values.OuterRectangleWidth);
            shader.SetParameter("outerRectangleHeight", values.OuterRectangleHeight);
            shader.SetParameter("innerRectangleThickness", values.InnerRectangleThickness);
            shader.SetParameter("alphaOuter", values.OuterAlpha);
            shader.SetParameter("alphaInner", values.InnerAlpha);
            handle.UseShader(shader);
            handle.DrawRect(args.WorldBounds, Color.White);
            handle.UseShader(null);
        }
    }
}

public struct RectangleShaderValues()
{
    public Vector3 RectColor = new(0, 0, 0);
    public float OuterRectangleWidth = 1;
    public float OuterRectangleHeight = 1;
    public float InnerRectangleThickness = 0.05f;
    public float OuterAlpha = 1.0f;
    public float InnerAlpha = 0.0f;

}
