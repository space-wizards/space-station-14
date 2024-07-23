﻿using Content.Shared.Lock;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Security.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Security.Systems;

public sealed class DeployableBarrierSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeployableBarrierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DeployableBarrierComponent, LockToggledEvent>(OnLockToggled);
    }

    private void OnMapInit(EntityUid uid, DeployableBarrierComponent component, MapInitEvent args)
    {
        if (!TryComp(uid, out LockComponent? lockComponent))
            return;

        ToggleBarrierDeploy(uid, lockComponent.Locked, component);
    }

    private void OnLockToggled(EntityUid uid, DeployableBarrierComponent component, ref LockToggledEvent args)
    {
        ToggleBarrierDeploy(uid, args.Locked, component);
    }

    private void ToggleBarrierDeploy(EntityUid uid, bool isDeployed, DeployableBarrierComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var transform = Transform(uid);
        var fixture = _fixtures.GetFixtureOrNull(uid, component.FixtureId);

        if (isDeployed && transform.GridUid != null)
        {
            _transform.AnchorEntity(uid, transform);
            if (fixture != null)
                _physics.SetHard(uid, fixture, true);
        }
        else
        {
            _transform.Unanchor(uid, transform);
            if (fixture != null)
                _physics.SetHard(uid, fixture, false);
        }

        if (TryComp(uid, out PullableComponent? pullable))
            _pulling.TryStopPull(uid, pullable);

        SharedPointLightComponent? pointLight = null;
        if (_pointLight.ResolveLight(uid, ref pointLight))
        {
            _pointLight.SetEnabled(uid, isDeployed, pointLight);
        }
    }
}
