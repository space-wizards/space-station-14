using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.TrueBlindness;

public sealed class TrueBlindnessOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    public override bool RequestScreenTexture => true;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly ShaderInstance _stencilShader;
    private readonly ShaderInstance _greyscaleShader;

    public bool Enabled => _enabled;

    private bool _enabled;

    public TrueBlindnessOverlay()
    {
        IoCManager.InjectDependencies(this);
        _stencilShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").InstanceUnique();
        _greyscaleShader = _prototypeManager.Index<ShaderPrototype>("GreyscaleFullscreen").InstanceUnique();

        ZIndex = (int)DrawDepth.Overlays;
    }

    public void SetEnabled(bool enabled)
    {
        _lightManager.Enabled = !enabled;
        _enabled = enabled;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_enabled)
            return false;

        var draw = true;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp)
            || args.Viewport.Eye != eyeComp.Eye
            || playerEntity is null)
            draw = false;

        return draw;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var content))
        {
            _stencilShader.SetParameter("Zoom", content.Zoom.X);
        }

        _stencilShader.SetParameter("CircleRadius", 22.5f);
        _greyscaleShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_greyscaleShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(_stencilShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
