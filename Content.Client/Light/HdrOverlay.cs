using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Light;

public sealed class HdrOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override bool RequestScreenTexture => true;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public HdrOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.Viewport;

        if (viewport.Eye == null || ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var bounds = args.WorldBounds;
        var shader = _protoManager.Index<ShaderPrototype>("ToneMapping").InstanceUnique();

        worldHandle.RenderInRenderTarget(viewport.RenderTarget,
            () =>
            {
                var invMatrix = viewport.RenderTarget.GetWorldToLocalMatrix(viewport.Eye, viewport.RenderScale);
                worldHandle.SetTransform(invMatrix);
                shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                shader.SetParameter("exposure", 1f);
                //worldHandle.UseShader(shader);
                worldHandle.DrawTextureRect(ScreenTexture, bounds);
            }, null);
    }
}
