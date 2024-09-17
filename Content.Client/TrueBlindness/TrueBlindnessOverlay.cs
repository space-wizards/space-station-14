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

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly ShaderInstance _stencilShader;

    public bool Enabled => _enabled;

    private bool _enabled = false;

    public TrueBlindnessOverlay()
    {
        IoCManager.InjectDependencies(this);
        _stencilShader = _prototypeManager.Index<ShaderPrototype>("StencilBlack").InstanceUnique();

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
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;


        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_stencilShader);
        worldHandle.DrawRect(viewport, Color.Black);
        worldHandle.UseShader(null);
    }
}
