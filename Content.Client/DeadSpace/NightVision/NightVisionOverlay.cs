using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.NightVision;

namespace Content.Client.DeadSpace.NightVision;

public sealed class NightVisionOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _greyscaleShader;
    private readonly ShaderInstance _circleMaskShader;

    private NightVisionComponent _nightVisionComponent = default!;

    public NightVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _greyscaleShader = _prototypeManager.Index<ShaderPrototype>("GreyscaleFullscreen").InstanceUnique();
        _circleMaskShader = _prototypeManager.Index<ShaderPrototype>("CircleMask").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        if (!_entityManager.TryGetComponent<NightVisionComponent>(playerEntity, out var nvComp))
            return false;

        _nightVisionComponent = nvComp;

        var nightVision = _nightVisionComponent.IsNightVision;

        if (!nightVision && _nightVisionComponent.LightSetup)
        {
            _lightManager.DrawLighting = true;
            _nightVisionComponent.LightSetup = false;
            _nightVisionComponent.GraceFrame = true;
            return true;
        }

        return nightVision;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        if (!_nightVisionComponent.GraceFrame)
        {
            _nightVisionComponent.LightSetup = true;
            _lightManager.DrawLighting = false;
        } else
        {
            _nightVisionComponent.GraceFrame = false;
        }

        if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var content))
        {
            _circleMaskShader?.SetParameter("Zoom", content.Zoom.X / 14); // Neh, but looks nice
        }

        _greyscaleShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_greyscaleShader);
        worldHandle.DrawRect(viewport, _nightVisionComponent.Color);
        worldHandle.UseShader(_circleMaskShader);
        worldHandle.DrawRect(viewport, Color.Gray);
        worldHandle.UseShader(null);
    }
}
