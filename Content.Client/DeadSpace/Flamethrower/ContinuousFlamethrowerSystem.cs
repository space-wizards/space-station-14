using Content.Client.CombatMode;
using Content.Client.Hands.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.DeadSpace.Flamethrower;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Client.DeadSpace.Flamethrower;

public sealed class ContinuousFlamethrowerSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly CombatModeSystem _combatMode = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;

    private const int MaxFlameLights = 12;
    private const float FlameLightLifetime = 0.22f;
    private readonly List<FlameLight> _flameLights = new();
    private FlamethrowerOverlay _overlay = default!;
    private EntityUid? _activeWeapon;
    private bool _firing;
    private float _sendTimer;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new FlamethrowerOverlay(_random, _resources);
        _overlays.AddOverlay(_overlay);
        SubscribeNetworkEvent<FlamethrowerVisualEvent>(OnVisual);

        CommandBinds.Builder
            .BindBefore(EngineKeyFunctions.Use,
                new PointerInputCmdHandler(OnUse, false, true),
                typeof(Content.Shared.Interaction.SharedInteractionSystem))
            .Register<ContinuousFlamethrowerSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<ContinuousFlamethrowerSystem>();
        _overlays.RemoveOverlay(_overlay);
        foreach (var flameLight in _flameLights)
            Del(flameLight.Entity);
        _flameLights.Clear();
        base.Shutdown();
    }

    private bool OnUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity == null)
            return false;

        if (args.State == BoundKeyState.Up)
        {
            var handled = _activeWeapon != null;
            StopFiring(args.Coordinates);
            return handled;
        }

        var weapon = _hands.GetActiveHandEntity();
        if (weapon == null || !HasComp<ContinuousFlamethrowerComponent>(weapon.Value))
            return false;

        if (args.State != BoundKeyState.Down ||
            !_combatMode.IsInCombatMode() ||
            !_actionBlocker.CanAttack(args.Session.AttachedEntity.Value))
            return false;

        _firing = true;
        _activeWeapon = weapon;
        _sendTimer = 0.1f;
        RaiseNetworkEvent(new FlamethrowerInputEvent(
            GetNetEntity(weapon.Value),
            GetNetCoordinates(args.Coordinates),
            true));
        return true;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        _overlay.Update(frameTime);
        UpdateFlameLightLifetimes(frameTime);

        if (!_firing || _activeWeapon is not { } weapon)
            return;

        if (!Exists(weapon))
        {
            _firing = false;
            _activeWeapon = null;
            return;
        }

        if (!_combatMode.IsInCombatMode())
        {
            StopFiring(Transform(weapon).Coordinates);
            return;
        }

        if (_hands.GetActiveHandEntity() != weapon)
        {
            StopFiring(Transform(weapon).Coordinates);
            return;
        }

        _sendTimer -= frameTime;
        if (_sendTimer > 0f)
            return;

        _sendTimer = 0.1f;
        var mousePosition = _input.MouseScreenPosition;
        if (!mousePosition.IsValid)
            return;

        var coords = _eye.PixelToMap(mousePosition);
        if (coords.MapId == MapId.Nullspace)
            return;

        RaiseNetworkEvent(new FlamethrowerInputEvent(
            GetNetEntity(weapon),
            GetNetCoordinates(_transform.ToCoordinates(coords)),
            true));
    }

    private void OnVisual(FlamethrowerVisualEvent args)
    {
        var points = new List<MapCoordinates>(args.Points.Count);
        foreach (var point in args.Points)
            points.Add(_transform.ToMapCoordinates(GetCoordinates(point)));
        _overlay.Add(points);
        UpdateFlameLights(points);
    }

    private void UpdateFlameLights(List<MapCoordinates> points)
    {
        if (points.Count == 0)
            return;

        var wanted = Math.Min(MaxFlameLights, Math.Max(1, points.Count / 2));
        while (_flameLights.Count < wanted)
        {
            var entity = Spawn(null, points[0]);
            var light = _lights.EnsureLight(entity);
            _lights.SetColor(entity, new Color(1f, 0.18f, 0.01f), light);
            _lights.SetRadius(entity, 2.35f, light);
            _lights.SetEnergy(entity, 3.2f, light);
            _lights.SetSoftness(entity, 0.85f, light);
            _lights.SetCastShadows(entity, false, light);
            _lights.SetEnabled(entity, true, light);
            _flameLights.Add(new FlameLight(entity));
        }

        for (var i = 0; i < wanted; i++)
        {
            var pointIndex = wanted == 1
                ? 0
                : i * (points.Count - 1) / (wanted - 1);
            var flameLight = _flameLights[i];
            _transform.SetMapCoordinates(flameLight.Entity, points[pointIndex]);
            flameLight.Remaining = FlameLightLifetime;
        }
    }

    private void UpdateFlameLightLifetimes(float frameTime)
    {
        for (var i = _flameLights.Count - 1; i >= 0; i--)
        {
            var flameLight = _flameLights[i];
            flameLight.Remaining -= frameTime;
            if (flameLight.Remaining > 0f)
                continue;

            Del(flameLight.Entity);
            _flameLights.RemoveAt(i);
        }
    }
    private void StopFiring(EntityCoordinates target)
    {
        var weapon = _activeWeapon;
        var wasFiring = _firing;
        _firing = false;
        _activeWeapon = null;
        _sendTimer = 0f;

        if (!wasFiring || weapon == null || !Exists(weapon.Value))
            return;

        RaiseNetworkEvent(new FlamethrowerInputEvent(
            GetNetEntity(weapon.Value),
            GetNetCoordinates(target),
            false));
    }

    private sealed class FlameLight(EntityUid entity)
    {
        public EntityUid Entity = entity;
        public float Remaining = FlameLightLifetime;
    }
}
