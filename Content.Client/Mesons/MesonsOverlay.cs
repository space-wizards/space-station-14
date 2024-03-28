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
    // [Dependency] private readonly IClydeViewport _clydeViewport = default!;
    //
    // [Dependency] private readonly InventorySystem _inventory = default!;

    public new int? ZIndex = -5;
    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;
    private readonly ShaderInstance _mesonsShader;

    public MesonsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _mesonsShader = _prototypeManager.Index<ShaderPrototype>("Mesons").InstanceUnique();
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

        // _mesonsShader.SetParameter("LIGHT_TEXTURE", _clydeViewport.LightRenderTarget.Texture);

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.UseShader(_mesonsShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}
