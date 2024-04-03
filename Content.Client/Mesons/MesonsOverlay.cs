using System.Numerics;
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
    [Dependency] private readonly IClyde _clyde = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _scanlineShader;
    private readonly ShaderInstance _outlineShader;


    public MesonsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _scanlineShader = _prototypeManager.Index<ShaderPrototype>("Scanline").InstanceUnique();
        _outlineShader = _prototypeManager.Index<ShaderPrototype>("BitMaskOutline").InstanceUnique();
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

        IRenderHandle _outlineRenderHandle = new();

        DrawingHandleWorld _outlineHandle = _outlineRenderHandle.DrawingHandleWorld;

        _outlineHandle.DrawTexture(ScreenTexture, Vector2.Zero);

        var target = _clyde.CreateRenderTarget(_clyde.ScreenSize,
            new RenderTargetFormatParameters(RenderTargetColorFormat.R8));

        while (query.MoveNext(out var uid, out _))
        {
            if (!_entityManager.TryGetComponent(uid, out SpriteComponent? sprite))
                continue;

            sprite.Render(
                _outlineHandle,
                Angle.Zero,
                Angle.Zero
                );
        }

        _outlineHandle.RenderInRenderTarget(target, () => {}, Color.Red);

        _scanlineShader.SetParameter("OVERLAY_COLOR", new Color(0f, 0.2f, 0f));

        _outlineShader.SetParameter("OVERLAY_COLOR", new Color(1f, 1f, 1f, 0f));
        _outlineShader.SetParameter("MASK_TEXTURE", target.Texture);

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_scanlineShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(_outlineShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
