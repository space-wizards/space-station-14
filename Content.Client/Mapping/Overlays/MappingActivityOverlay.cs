using Content.Client.Hands;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;

namespace Content.Client.Mapping.Overlays;

public sealed class MappingActivityOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private readonly IRenderTexture _renderBackbuffer;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public Texture? IconOverride;
    public EntityUid? EntityOverride;

    public MappingActivityOverlay()
    {
        IoCManager.InjectDependencies(this);

        _renderBackbuffer = _clyde.CreateRenderTarget(
            (64, 64),
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
            new TextureSampleParameters
            {
                Filter = true
            }, nameof(MappingActivityOverlay));
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();

        _renderBackbuffer.Dispose();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var screen = args.ScreenHandle;
        var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);
        var mousePos = _inputManager.MouseScreenPosition.Position;
    }
}
