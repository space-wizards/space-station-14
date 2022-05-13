using Content.Server.Lock;
using Content.Server.Storage.Components;
using Content.Shared.Security;
using Robust.Server.GameObjects;

namespace Content.Server.Security.Systems
{
    public sealed class DeployableBarrierSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DeployableBarrierComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<DeployableBarrierComponent, LockToggledEvent>(OnLockToggled);
        }

        private void OnStartup(EntityUid uid, DeployableBarrierComponent component, ComponentStartup args)
        {
            if (!EntityManager.TryGetComponent(component.Owner, out LockComponent? lockComponent))
                return;

            ToggleBarrierDeploy(component, lockComponent.Locked);
        }

        private void OnLockToggled(EntityUid uid, DeployableBarrierComponent component, LockToggledEvent args)
        {
            ToggleBarrierDeploy(component, args.Locked);
        }

        private void ToggleBarrierDeploy(DeployableBarrierComponent component, bool isDeployed)
        {
            EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored = isDeployed;

            if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent))
                return;

            var state = isDeployed ? DeployableBarrierState.Deployed : DeployableBarrierState.Idle;
            appearanceComponent.SetData(DeployableBarrierVisuals.State, state);

            if (EntityManager.TryGetComponent(component.Owner, out PointLightComponent? light))
                light.Enabled = isDeployed;
        }
    }
}
