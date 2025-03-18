using System.Linq;
using System.Numerics;
using Content.Client.Stealth;
using Content.Shared.Body.Components;
using Content.Shared._EE.Overlays.Switchable;
using Content.Shared.Stealth.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client._EE.Overlays.Switchable;

public sealed class ThermalVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly TransformSystem _transform;
    private readonly StealthSystem _stealth;
    private readonly ContainerSystem _container;
    private readonly SharedPointLightSystem _light;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly List<ThermalVisionRenderEntry> _entries = [];

    private EntityUid? _lightEntity;

    public float LightRadius;

    public ThermalVisionComponent? Comp;

    public ThermalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _container = _entity.System<ContainerSystem>();
        _transform = _entity.System<TransformSystem>();
        _stealth = _entity.System<StealthSystem>();
        _light = _entity.System<SharedPointLightSystem>();

        ZIndex = -1;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null || Comp is null)
            return;

        var worldHandle = args.WorldHandle;
        var eye = args.Viewport.Eye;

        if (eye == null)
            return;

        var player = _player.LocalEntity;

        if (!_entity.TryGetComponent(player, out TransformComponent? playerXform))
            return;

        var accumulator = Math.Clamp(Comp.PulseAccumulator, 0f, Comp.PulseTime);
        var alpha = Comp.PulseTime <= 0f ? 1f : float.Lerp(1f, 0f, accumulator / Comp.PulseTime);

        // Thermal vision grants some night vision (clientside light)
        if (LightRadius > 0)
        {
            _lightEntity ??= _entity.SpawnAttachedTo(null, playerXform.Coordinates);
            _transform.SetParent(_lightEntity.Value, player.Value);
            var light = _entity.EnsureComponent<PointLightComponent>(_lightEntity.Value);
            _light.SetRadius(_lightEntity.Value, LightRadius, light);
            _light.SetEnergy(_lightEntity.Value, alpha, light);
            _light.SetColor(_lightEntity.Value, Comp.Color, light);
        }
        else
            ResetLight();

        var mapId = eye.Position.MapId;
        var eyeRot = eye.Rotation;

        _entries.Clear();
        var entities = _entity.EntityQueryEnumerator<BodyComponent, SpriteComponent, TransformComponent>();
        while (entities.MoveNext(out var uid, out var body, out var sprite, out var xform))
        {
            if (!CanSee(uid, sprite) || !body.ThermalVisibility)
                continue;

            var entity = uid;

            if (_container.TryGetOuterContainer(uid, xform, out var container))
            {
                var owner = container.Owner;
                if (_entity.TryGetComponent<SpriteComponent>(owner, out var ownerSprite)
                    && _entity.TryGetComponent<TransformComponent>(owner, out var ownerXform))
                {
                    entity = owner;
                    sprite = ownerSprite;
                    xform = ownerXform;
                }
            }

            if (_entries.Any(e => e.Ent.Owner == entity))
                continue;

            _entries.Add(new ThermalVisionRenderEntry((entity, sprite, xform), mapId, eyeRot));
        }

        foreach (var entry in _entries)
        {
            Render(entry.Ent, entry.Map, worldHandle, entry.EyeRot, Comp.Color, alpha);
        }

        worldHandle.SetTransform(Matrix3x2.Identity);
    }

    private void Render(Entity<SpriteComponent, TransformComponent> ent,
        MapId? map,
        DrawingHandleWorld handle,
        Angle eyeRot,
        Color color,
        float alpha)
    {
        var (uid, sprite, xform) = ent;
        if (xform.MapID != map || !CanSee(uid, sprite))
            return;

        var position = _transform.GetWorldPosition(xform);
        var rotation = _transform.GetWorldRotation(xform);

        var originalColor = sprite.Color;
        sprite.Color = color.WithAlpha(alpha);
        sprite.Render(handle, eyeRot, rotation, position: position);
        sprite.Color = originalColor;
    }

    private bool CanSee(EntityUid uid, SpriteComponent sprite)
    {
        return sprite.Visible && (!_entity.TryGetComponent(uid, out StealthComponent? stealth) ||
                                  _stealth.GetVisibility(uid, stealth) > 0.5f);
    }

    public void ResetLight(bool checkFirstTimePredicted = true)
    {
        if (_lightEntity == null || checkFirstTimePredicted && !_timing.IsFirstTimePredicted)
            return;

        _entity.DeleteEntity(_lightEntity);
        _lightEntity = null;
    }
}

public record struct ThermalVisionRenderEntry(
    Entity<SpriteComponent, TransformComponent> Ent,
    MapId? Map,
    Angle EyeRot);
