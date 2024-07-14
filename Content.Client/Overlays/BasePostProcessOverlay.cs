using Content.Shared.CCVar;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

// This overlay serves as the foundational post processing overlay.
// Ideally, for performance reasons, post processing designed to be present at all times, such as additive light blending or tonemapping, should be done as part of a single shader pass.
public sealed class BasePostProcessOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _basePostProcessShader;

    public BasePostProcessOverlay()
    {
        IoCManager.InjectDependencies(this);
        _basePostProcessShader = _prototypeManager.Index<ShaderPrototype>("BasePostProcess").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_configManager.GetCVar(CCVars.PostProcess))
            return false;

        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        if (!_lightManager.Enabled || !eyeComp.Eye.DrawLight)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        var zoom = 1.0f;
        if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var eyeComponent))
        {
            zoom = eyeComponent.Zoom.X;
        }

        _basePostProcessShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _basePostProcessShader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);

        _basePostProcessShader.SetParameter("Zoom", zoom);

        worldHandle.UseShader(_basePostProcessShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}