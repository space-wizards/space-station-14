using Content.Shared.Mesons;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client.Mesons;

public sealed class MesonsOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILightManager _lightManager = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _scanlineShader;
    private readonly ShaderInstance _brightnessShader;


    public MesonsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _scanlineShader = _prototypeManager.Index<ShaderPrototype>("Scanline").InstanceUnique();
        _brightnessShader = _prototypeManager.Index<ShaderPrototype>("BrightnessFilter").InstanceUnique();
    }
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _lightManager.Enabled = true;

        var query = _entityManager.EntityQueryEnumerator<MesonsNonviewableComponent>();

        var draw = true;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp)
            || args.Viewport.Eye != eyeComp.Eye
            || playerEntity is null)
            draw = false;

        while (query.MoveNext(out var uid, out _))
            _entityManager.EnsureComponent<NoRenderInWorldComponent>(uid).Enabled = uid == playerEntity || draw;

        return draw;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;

        _lightManager.Enabled = false;

        _scanlineShader.SetParameter("OVERLAY_COLOR", new Color(0f, 0.2f, 0f, 0.5f));
        _brightnessShader.SetParameter("THRESHHOLD", 0.1f);
        _brightnessShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);


        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_brightnessShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(_scanlineShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
