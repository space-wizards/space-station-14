using Content.Server.Lock;
using Content.Server.Storage.Components;
using Content.Shared.Security;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using System;

namespace Content.Server.Security.Systems
{
    public class DeployableBarrierSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DeployableBarrierComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DeployableBarrierComponent, LockToggledEvent>(OnLockToggled);
        }

        private void OnStartup(EntityUid uid, DeployableBarrierComponent component, ComponentStartup args)
        {
            if (!component.Owner.TryGetComponent(out LockComponent? lockComponent))
                return;

            ToggleBarrierDeploy(component, lockComponent.Locked);
        }

        private void OnLockToggled(EntityUid uid, DeployableBarrierComponent component, LockToggledEvent args)
        {
            ToggleBarrierDeploy(component, args.Locked);
        }

        private void ToggleBarrierDeploy(DeployableBarrierComponent component, bool isDeployed)
        {
            component.Owner.Transform.Anchored = isDeployed;

            if (!component.Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
                return;

            var state = isDeployed ? DeployableBarrierState.Deployed : DeployableBarrierState.Idle;
            appearanceComponent.SetData(DeployableBarrierVisuals.State, state);

            if (component.Owner.TryGetComponent(out PointLightComponent? light))
                light.Enabled = isDeployed;
        }
    }
}
