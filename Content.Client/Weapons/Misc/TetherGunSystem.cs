using Content.Shared.Weapons.Misc;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Misc;

public sealed partial class TetherGunSystem : SharedTetherGunSystem
{
    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private MapSystem _mapSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetheredComponent, ComponentStartup>(OnTetheredStartup);
        SubscribeLocalEvent<TetheredComponent, ComponentShutdown>(OnTetheredShutdown);
        SubscribeLocalEvent<BaseForceGunComponent, AfterAutoHandleStateEvent>(OnAfterState);
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
            !TryGetGun(player.Value, out _, out var gun) ||
            !Exists(gun.TetherEntity))
        {
            return;
        }

        var mousePos = _input.MouseScreenPosition;
        var mouseWorldPos = _eyeManager.PixelToMap(mousePos);

        if (mouseWorldPos.MapId == MapId.Nullspace)
            return;

        if (!TryGetCoords(mouseWorldPos, out var coords))
            return;

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

        if (TryComp<BaseForceGunComponent>(component.Tetherer, out var force))
        {
            _sprite.SetColor((uid, sprite), force.LineColor);
        }
    }

    private void OnTetheredShutdown(EntityUid uid, TetheredComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _sprite.SetColor((uid, sprite), Color.White);
    }
}
