using Content.Server.Lock;
using Content.Server.Storage.Components;
using Content.Shared.Security;
using Robust.Server.GameObjects;

namespace Content.Server.Security.Systems
{
    public sealed class DeployableBarrierSystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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

            ToggleBarrierDeploy(uid, component, lockComponent.Locked);
        }

        private void OnLockToggled(EntityUid uid, DeployableBarrierComponent component, LockToggledEvent args)
        {
            ToggleBarrierDeploy(uid, component, args.Locked);
        }

        private void ToggleBarrierDeploy(EntityUid uid, DeployableBarrierComponent component, bool isDeployed)
        {
            EntityManager.GetComponent<TransformComponent>(component.Owner).Anchored = isDeployed;

            if (!EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
                return;

            var state = isDeployed ? DeployableBarrierState.Deployed : DeployableBarrierState.Idle;
            _appearance.SetData(uid, DeployableBarrierVisuals.State, state, appearance);

            if (EntityManager.TryGetComponent(component.Owner, out PointLightComponent? light))
                light.Enabled = isDeployed;
        }
    }
}
