using Content.Shared.Mesons;
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
    [Dependency] private readonly IClyde _clyde = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _scanlineShader;
    private readonly ShaderInstance _bitMaskShader;
    private readonly IRenderTexture _bitMask;

    public MesonsOverlay()
    {
        IoCManager.InjectDependencies(this);

        _bitMask = _clyde.CreateRenderTarget(
            _clyde.ScreenSize,
            new RenderTargetFormatParameters(RenderTargetColorFormat.R8));
        _scanlineShader = _prototypeManager.Index<ShaderPrototype>("Scanline").InstanceUnique();
        _bitMaskShader = _prototypeManager.Index<ShaderPrototype>("BitMaskOutline").InstanceUnique();
    }
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;



        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity is null)
            return false;

        // if (!_inventory.TryGetSlotContainer(playerEntity.Value, "eyes", out var container, out _) ||
        //     !_entityManager.TryGetComponent(container.ContainedEntity, out MesonsComponent? mesonsComponent) ||
        //     mesonsComponent is not { MesonsType: MesonsViewType.Walls })
        //     return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return;


        var query = _entityManager.EntityQueryEnumerator<MesonsViewableComponent>();

        while (query.MoveNext(out var uid, out _))
        {

        }

        _scanlineShader.SetParameter("OVERLAY_COLOR", new Color(0f, 0.2f, 0f));

        _bitMaskShader.SetParameter("OVERLAY_COLOR", new Color(1f, 1f, 1f));

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_scanlineShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(_bitMaskShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
