using Content.Client.Fov.Overlays;
using Content.Shared.Fov.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Fov.Systems;

public sealed class FovLimiterSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedEyeSystem _eyeSystem = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ConeFovOverlay? _coneOverlay;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FovLimiterComponent, ComponentStartup>(OnFovLimiterStartup);
        SubscribeLocalEvent<FovLimiterComponent, ComponentShutdown>(OnFovLimiterShutdown);
        SubscribeLocalEvent<FovLimiterComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FovLimiterComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnFovLimiterStartup(EntityUid uid, FovLimiterComponent component, ComponentStartup args)
    {
        UpdateConeOverlayState();
        if (_playerManager.LocalPlayer?.ControlledEntity == uid)
            UpdateFovLimitation(component);
    }

    private void OnFovLimiterShutdown(EntityUid uid, FovLimiterComponent component, ComponentShutdown args)
    {
        if (_playerManager.LocalPlayer?.ControlledEntity == uid)
        {
            // Reset to default FOV when component is removed
            if (TryComp<EyeComponent>(uid, out var eye))
            {
                _eyeSystem.SetDrawFov(uid, true, eye);
            }
        }

        UpdateConeOverlayState();
    }

    private void OnPlayerAttached(EntityUid uid, FovLimiterComponent component, PlayerAttachedEvent args)
    {
        UpdateFovLimitation(component);
        UpdateConeOverlayState();
    }

    private void OnPlayerDetached(EntityUid uid, FovLimiterComponent component, PlayerDetachedEvent args)
    {
        // Reset FOV when detaching
        if (TryComp<EyeComponent>(uid, out var eye))
        {
            _eyeSystem.SetDrawFov(uid, true, eye);
        }

        UpdateConeOverlayState();
    }

    private void UpdateFovLimitation(FovLimiterComponent component)
    {
        if (!component.Enabled || _playerManager.LocalPlayer?.ControlledEntity is not { } player)
            return;

        if (TryComp<EyeComponent>(player, out var eye))
        {
            // Convert FOV from degrees to the game's internal representation if needed
            // The exact conversion depends on how SS14 handles FOV internally
            // This is a placeholder - you'll need to adjust based on the actual FOV system
            _eyeSystem.SetDrawFov(player, true, eye);

            // If there's a way to set specific FOV limits in the EyeComponent, you would do it here
            // For example:
            // eye.Fov = Math.Clamp(eye.Fov, 0, component.FovLimit);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Apply FOV limitation to all players if needed
        var query = EntityQueryEnumerator<FovLimiterComponent, EyeComponent>();
        var overlayShouldBeOn = false;

        while (query.MoveNext(out var uid, out var limiter, out var eye))
        {
            if (!limiter.Enabled)
                continue;

            if (!limiter.ApplyToAllPlayers && _playerManager.LocalPlayer?.ControlledEntity != uid)
                continue;

            overlayShouldBeOn = true;
            // Ensure overlay settings reflect this limiter.
            EnsureConeOverlay();
            if (_coneOverlay != null)
            {
                _coneOverlay.AngleDegrees = 120f;
                _coneOverlay.RotationOffsetDegrees = -90f;
                _coneOverlay.OutsideOpacity = 0.7f;
                _coneOverlay.CenterClearRadius = 0.75f;
            }

            // Apply FOV limitation
            // This is where you would apply the actual FOV limitation
            // The exact implementation depends on how SS14 handles FOV
        }

        // Toggle overlay based on state this frame.
        if (overlayShouldBeOn)
            EnsureConeOverlay(added: true);
        else
            RemoveConeOverlay();
    }

    private void EnsureConeOverlay(bool added = true)
    {
        if (_coneOverlay == null)
        {
            _coneOverlay = new ConeFovOverlay(_eyeManager, _prototypeManager)
            {
                AngleDegrees = 120f,
                RotationOffsetDegrees = -90f,
                OutsideOpacity = 0.7f,
                CenterClearRadius = 0.75f
            };
        }

        if (added && !_overlayManager.HasOverlay<ConeFovOverlay>())
            _overlayManager.AddOverlay(_coneOverlay);
    }

    private void RemoveConeOverlay()
    {
        if (_overlayManager.HasOverlay<ConeFovOverlay>())
            _overlayManager.RemoveOverlay<ConeFovOverlay>();
    }

    private void UpdateConeOverlayState()
    {
        var query = EntityQueryEnumerator<FovLimiterComponent>();
        var overlayShouldBeOn = false;

        while (query.MoveNext(out var uid, out var limiter))
        {
            if (!limiter.Enabled)
                continue;

            if (!limiter.ApplyToAllPlayers && _playerManager.LocalPlayer?.ControlledEntity != uid)
                continue;

            overlayShouldBeOn = true;
        }

        // Toggle overlay based on state this frame.
        if (overlayShouldBeOn)
            EnsureConeOverlay(added: true);
        else
            RemoveConeOverlay();
    }
}