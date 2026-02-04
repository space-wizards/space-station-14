using Content.Shared.Weapons.Misc;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Misc;

public sealed class TetherGunSystem : SharedTetherGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetheredComponent, ComponentStartup>(OnTetheredStartup);
        SubscribeLocalEvent<TetheredComponent, ComponentShutdown>(OnTetheredShutdown);
        SubscribeLocalEvent<TetherGunComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<ForceGunComponent, AfterAutoHandleStateEvent>(OnAfterState);
        _overlay.AddOverlay(new TetherGunOverlay(EntityManager));
    }

    private void OnAfterState(EntityUid uid, BaseForceGunComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(component.Tethered, out var sprite))
            return;

        _sprite.SetColor((component.Tethered.Value, sprite), component.LineColor);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<TetherGunOverlay>();
    }

    protected override bool CanTether(EntityUid uid, BaseForceGunComponent component, EntityUid target, EntityUid? user)
    {
        // Need powercells predicted sadly :<
        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var player = _player.LocalEntity;

        if (player == null ||
            !TryGetTetherGun(player.Value, out _, out var gun) ||
            gun.TetherEntity == null)
        {
            return;
        }

        var mousePos = _input.MouseScreenPosition;
        var mouseWorldPos = _eyeManager.PixelToMap(mousePos);

        if (mouseWorldPos.MapId == MapId.Nullspace)
            return;

        EntityCoordinates coords;

        if (_mapManager.TryFindGridAt(mouseWorldPos, out var gridUid, out _))
        {
            coords = TransformSystem.ToCoordinates(gridUid, mouseWorldPos);
        }
        else
        {
            coords = TransformSystem.ToCoordinates(_mapSystem.GetMap(mouseWorldPos.MapId), mouseWorldPos);
        }

        const float bufferDistance = 0.1f;

        if (TryComp(gun.TetherEntity, out TransformComponent? tetherXform) &&
            tetherXform.Coordinates.TryDistance(EntityManager, TransformSystem, coords, out var distance) &&
            distance < bufferDistance)
        {
            return;
        }

        RaisePredictiveEvent(new RequestTetherMoveEvent()
        {
            Coordinates = GetNetCoordinates(coords)
        });
    }

    private void OnTetheredStartup(EntityUid uid, TetheredComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        if (TryComp<ForceGunComponent>(component.Tetherer, out var force))
        {
            _sprite.SetColor((uid, sprite), force.LineColor);
        }
        else if (TryComp<TetherGunComponent>(component.Tetherer, out var tether))
        {
            _sprite.SetColor((uid, sprite), tether.LineColor);
        }
    }

    private void OnTetheredShutdown(EntityUid uid, TetheredComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetColor((uid, sprite), Color.White);
    }
}
