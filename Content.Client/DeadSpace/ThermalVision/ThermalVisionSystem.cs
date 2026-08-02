// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.ThermalVision;
using Content.Shared.Inventory;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.ThermalVision;

public sealed class ThermalVisorSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SpriteSystem _spriteSys = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private ThermalVisionOverlay _overlay = default!;
    private bool _wasActive;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalVisionComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentShutdown>(OnActiveShutdown);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ThermalVisionComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new ThermalVisionOverlay(EntityManager, _spriteSys, _lookup);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay(_overlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        var player = _player.LocalEntity;
        if (player == null || !TryComp<ThermalVisionComponent>(player, out var comp))
            return;

        if (comp.IsActive && !_wasActive)
            _audio.PlayLocal(comp.ActivateSound, player.Value, null);
        else if (!comp.IsActive && _wasActive)
            _audio.PlayLocal(comp.ActivateSoundOff, player.Value, null);
        _wasActive = comp.IsActive;
    }

    private void OnActiveInit(EntityUid uid, ThermalVisionComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            AddVision();
    }

    private void OnActiveShutdown(EntityUid uid, ThermalVisionComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _wasActive = false;
            RemVision();
        }
    }

    private void OnPlayerAttached(EntityUid uid, ThermalVisionComponent component, LocalPlayerAttachedEvent args)
    {
        AddVision();
    }

    private void OnPlayerDetached(EntityUid uid, ThermalVisionComponent component, LocalPlayerDetachedEvent args)
    {
        _wasActive = false;
        RemVision();
    }

    private void AddVision()
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void RemVision()
    {
        _overlayMan.RemoveOverlay(_overlay);
    }
}

public sealed class ThermalVisionOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly ShaderInstance _vignetteShader;
    private readonly SpriteSystem _spriteSys;
    private readonly EntityLookupSystem _lookup;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    public ThermalVisionOverlay(IEntityManager entityManager, SpriteSystem spriteSys, EntityLookupSystem lookup)
    {
        _entityManager = entityManager;
        _spriteSys = spriteSys;
        _lookup = lookup;
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var shaderProto = protoMan.Index(new ProtoId<ShaderPrototype>("ThermalMask"));
        _vignetteShader = shaderProto.InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
        if (player == null || !_entityManager.TryGetComponent<ThermalVisionComponent>(player.Value, out var comp))
            return false;

        if (!_entityManager.TryGetComponent<EyeComponent>(player.Value, out var eye) || args.Viewport.Eye != eye.Eye)
            return false;

        return comp.IsActive;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Space == OverlaySpace.ScreenSpace)
        {
            var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
            if (player == null || !_entityManager.TryGetComponent<ThermalVisionComponent>(player.Value, out var comp) || !comp.UseShader)
                return;

            var screenHandle = (DrawingHandleScreen)args.DrawingHandle;
            screenHandle.UseShader(_vignetteShader);
            screenHandle.DrawRect(args.ViewportBounds, Color.White);
            screenHandle.UseShader(null);
            return;
        }

        var worldHandle = (DrawingHandleWorld)args.DrawingHandle;
        var xformSys = _entityManager.System<SharedTransformSystem>();
        var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        var entities = _lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds.Enlarged(1f));
        foreach (var uid in entities)
        {
            if (!_entityManager.TryGetComponent<ThermalVisibleComponent>(uid, out _))
                continue;

            if (!_entityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
                continue;

            if (!_entityManager.TryGetComponent<TransformComponent>(uid, out var xform))
                continue;

            if (!sprite.Visible)
                continue;

            var drawUid = uid;
            var drawSprite = sprite;
            var drawXform = xform;

            var parentUid = xform.ParentUid;
            while (parentUid.IsValid() && !_entityManager.HasComponent<MapGridComponent>(parentUid))
            {
                if (_entityManager.HasComponent<InventoryComponent>(parentUid))
                {
                    drawUid = EntityUid.Invalid;
                    break;
                }

                if (_entityManager.TryGetComponent(parentUid, out SpriteComponent? parentSprite) &&
                    _entityManager.TryGetComponent(parentUid, out TransformComponent? parentXform))
                {
                    drawUid = parentUid;
                    drawSprite = parentSprite;
                    drawXform = parentXform;
                }

                parentUid = _entityManager.GetComponent<TransformComponent>(parentUid).ParentUid;
            }

            if (drawUid == EntityUid.Invalid)
                continue;

            var worldPos = xformSys.GetWorldPosition(drawXform);
            var worldRot = xformSys.GetWorldRotation(drawXform);

            var oldColor = drawSprite.Color;
            _spriteSys.SetColor((drawUid, drawSprite), new Color(
                oldColor.R,
                oldColor.G * 0.5f,
                oldColor.B * 0.5f,
                oldColor.A));
            _spriteSys.RenderSprite((drawUid, drawSprite), worldHandle, eyeRot, worldRot, worldPos);
            _spriteSys.SetColor((drawUid, drawSprite), oldColor);
        }
    }
}