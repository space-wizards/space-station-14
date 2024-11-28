using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Overlays;
using Robust.Shared.Timing;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Content.Shared.Clothing;

namespace Content.Client.Overlays
{
    public sealed class StaticViewerHudSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private ShaderInstance _staticViewerShader = null!;
private StaticViewerOverlay _staticViewerOverlay = null!;


    public StaticViewerHudSystem()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void Initialize()
    {
        _staticViewerShader = _prototypeManager.Index<ShaderPrototype>("Grainy").Instance();

        _staticViewerOverlay = new StaticViewerOverlay(_staticViewerShader, _entityManager, _player);

        SubscribeLocalEvent<StaticViewerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<StaticViewerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StaticViewerComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<StaticViewerComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<StaticViewerComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<StaticViewerComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<StaticViewerComponent> ent, ref GotEquippedEvent args)
    {
        EnsureComp<StaticViewerComponent>(args.Equipee);
    }

    private void OnUnequipped(Entity<StaticViewerComponent> ent, ref GotUnequippedEvent args)
    {
        _entityManager.RemoveComponent<StaticViewerComponent>(args.Equipee);
    }

    private void OnPlayerAttached(Entity<StaticViewerComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_staticViewerOverlay);
    }

    private void OnPlayerDetached(Entity<StaticViewerComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_staticViewerOverlay);
    }

    private void OnInit(Entity<StaticViewerComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlayMan.AddOverlay(_staticViewerOverlay);
        }
    }

    private void OnShutdown(Entity<StaticViewerComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent)
        {
            _overlayMan.RemoveOverlay(_staticViewerOverlay);
        }
    }
}

   public sealed class StaticViewerOverlay : Overlay
{
    private readonly ShaderInstance _shaderInstance;
    private readonly IEntityManager _entityManager;
    private readonly IPlayerManager _playerManager;

    public StaticViewerOverlay(ShaderInstance shaderInstance, IEntityManager entityManager, IPlayerManager playerManager)
    {
        _shaderInstance = shaderInstance;
        _entityManager = entityManager;
        _playerManager = playerManager;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (!playerEntity.HasValue)
            return false;

        if (!_entityManager.TryGetComponent(playerEntity.Value, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;

        // Duplicate the shader instance to make it mutable
        var mutableShaderInstance = _shaderInstance.Duplicate();

        mutableShaderInstance.SetParameter("SCREEN_TEXTURE", ScreenTexture);

        // Use the duplicated mutable shader
        handle.UseShader(mutableShaderInstance);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }
}
}

