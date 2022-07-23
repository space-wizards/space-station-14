using Content.Shared.Gravity;
using JetBrains.Annotations;

namespace Content.Server.Gravity.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GravitySystem : SharedGravitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GravityComponent, ComponentInit>(HandleGravityInitialize);
            SubscribeLocalEvent<GravityComponent, ComponentShutdown>(HandleGravityShutdown);
        }

        private void HandleGravityInitialize(EntityUid uid, GravityComponent component, ComponentInit args)
        {
            // Incase there's already a generator on the grid we'll just set it now.
            var gridId = Transform(component.Owner).GridUid;

            if (gridId == null)
                return;

            GravityChangedMessage message;

            foreach (var generator in EntityManager.EntityQuery<GravityGeneratorComponent>())
            {
                if (Transform(generator.Owner).GridUid == gridId && generator.GravityActive)
                {
                    component.Enabled = true;
                    message = new GravityChangedMessage(gridId.Value, true);
                    RaiseLocalEvent(message);
                    return;
                }
            }

            component.Enabled = false;
            message = new GravityChangedMessage(gridId.Value, false);
            RaiseLocalEvent(message);
        }

        private void HandleGravityShutdown(EntityUid uid, GravityComponent component, ComponentShutdown args)
        {
            DisableGravity(component);
        }

        public void EnableGravity(GravityComponent comp)
        {
            if (comp.Enabled) return;

            var gridId = Transform(comp.Owner).GridUid;
            if (gridId == null)
                return;

            comp.Enabled = true;
            var message = new GravityChangedMessage(gridId.Value, true);
            RaiseLocalEvent(message);
        }

        public void DisableGravity(GravityComponent comp)
        {
            if (!comp.Enabled) return;
            comp.Enabled = false;

            var gridId = Transform(comp.Owner).GridUid;
            if (gridId == null)
                return;

            var message = new GravityChangedMessage(gridId.Value, false);
            RaiseLocalEvent(message);
        }
    }
}
