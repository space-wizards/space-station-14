// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.ThermalVision;
using Content.Shared.Inventory;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.ThermalVision;

public sealed class ThermalVisionExperimentalSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly SpriteSystem _spriteSys = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedBatterySystem _batterySys = default!;

    private ThermalVisionExperimentalOverlay _overlay = default!;
    private bool _wasActive;
    private const float EnergyPerUse = 100f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThermalVisionExperimentalComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<ThermalVisionExperimentalComponent, ComponentShutdown>(OnActiveShutdown);
        SubscribeLocalEvent<ThermalVisionExperimentalComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ThermalVisionExperimentalComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _overlay = new ThermalVisionExperimentalOverlay(EntityManager, _spriteSys, _lookup);
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
        if (player != null && TryComp<ThermalVisionExperimentalComponent>(player, out var comp))
        {
            if (comp.IsActive)
            {
                comp.CurrentPulseTime -= frameTime;
                if (comp.CurrentPulseTime <= 0f)
                    comp.CurrentPulseTime = 0f;
            }

            if (comp.IsActive && !_wasActive)
                _audio.PlayLocal(comp.ActivateSound, player.Value, null);
            else if (!comp.IsActive && _wasActive)
                _audio.PlayLocal(comp.ActivateSoundOff, player.Value, null);
            _wasActive = comp.IsActive;
        }

        var query = EntityQueryEnumerator<ThermalVisorExperimentalComponent, SpriteComponent, BatteryComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var battery))
        {
            UpdateVisorIndicator(uid, battery);
        }
    }

    private void OnActiveInit(EntityUid uid, ThermalVisionExperimentalComponent component, ComponentInit args)
    {
        if (_player.LocalEntity == uid)
            AddVision();
    }

    private void OnActiveShutdown(EntityUid uid, ThermalVisionExperimentalComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity == uid)
        {
            _wasActive = false;
            RemVision();
        }
    }

    private void OnPlayerAttached(EntityUid uid, ThermalVisionExperimentalComponent component, LocalPlayerAttachedEvent args)
    {
        AddVision();
    }

    private void OnPlayerDetached(EntityUid uid, ThermalVisionExperimentalComponent component, LocalPlayerDetachedEvent args)
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

    private void UpdateVisorIndicator(EntityUid uid, BatteryComponent battery)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var charge = _batterySys.GetCharge((uid, battery));
        var uses = (int)(charge / EnergyPerUse);

        string stateSuffix;
        if (uses >= 7)
            stateSuffix = "green";
        else if (uses >= 5)
            stateSuffix = "yellow";
        else if (uses >= 3)
            stateSuffix = "orange";
        else
            stateSuffix = "red";

        if (sprite.LayerMapTryGet("indicator_icon", out var indicatorLayer))
        {
            sprite.LayerSetState(indicatorLayer, new RSI.StateId($"indicator-{stateSuffix}"));
            sprite.LayerSetVisible(indicatorLayer, true);
        }
    }
}

public sealed class ThermalVisionExperimentalOverlay : Overlay
{
    private readonly IEntityManager _entityManager;
    private readonly ShaderInstance _vignetteShader;
    private readonly SpriteSystem _spriteSys;
    private readonly EntityLookupSystem _lookup;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    public ThermalVisionExperimentalOverlay(IEntityManager entityManager, SpriteSystem spriteSys, EntityLookupSystem lookup)
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
        if (player == null || !_entityManager.TryGetComponent<ThermalVisionExperimentalComponent>(player.Value, out _))
            return false;

        if (!_entityManager.TryGetComponent<EyeComponent>(player.Value, out var eye) || args.Viewport.Eye != eye.Eye)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Space == OverlaySpace.ScreenSpace)
        {
            var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
            if (player == null || !_entityManager.TryGetComponent<ThermalVisionExperimentalComponent>(player.Value, out var comp) || !comp.UseShader)
                return;

            var screenHandle = (DrawingHandleScreen)args.DrawingHandle;
            screenHandle.UseShader(_vignetteShader);
            screenHandle.DrawRect(args.ViewportBounds, Color.White);
            screenHandle.UseShader(null);
            return;
        }

        var player2 = IoCManager.Resolve<IPlayerManager>().LocalEntity;
        if (player2 == null || !_entityManager.TryGetComponent<ThermalVisionExperimentalComponent>(player2.Value, out var comp2))
            return;

        if (!comp2.IsActive)
            return;

        var worldHandle = (DrawingHandleWorld)args.DrawingHandle;
        var xformSys = _entityManager.System<SharedTransformSystem>();
        var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        var fadeTime = Math.Min(comp2.PulseDuration, 1f);
        var alpha = comp2.CurrentPulseTime > fadeTime ? 1f : Math.Clamp(comp2.CurrentPulseTime / fadeTime, 0f, 1f);

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
                oldColor.R * alpha,
                oldColor.G * 0.5f * alpha,
                oldColor.B * 0.5f * alpha,
                oldColor.A * alpha));
            _spriteSys.RenderSprite((drawUid, drawSprite), worldHandle, eyeRot, worldRot, worldPos);
            _spriteSys.SetColor((drawUid, drawSprite), oldColor);
        }
    }
}