using Content.Shared.Gravity;
using JetBrains.Annotations;

namespace Content.Server.Gravity.EntitySystems
{
    [UsedImplicitly]
    public sealed class GravitySystem : SharedGravitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GravityComponent, ComponentInit>(OnGravityInit);
            SubscribeLocalEvent<GravityComponent, ComponentShutdown>(OnGravityShutdown);
        }

        private void OnGravityInit(EntityUid uid, GravityComponent component, ComponentInit args)
        {
            // Incase there's already a generator on the grid we'll just set it now.
            var gridId = Transform(component.Owner).GridUid;

            if (gridId == null)
                return;

            GravityChangedEvent message;

            foreach (var generator in EntityManager.EntityQuery<GravityGeneratorComponent>())
            {
                if (Transform(generator.Owner).GridUid == gridId && generator.GravityActive)
                {
                    component.Enabled = true;
                    message = new GravityChangedEvent(gridId.Value, true);
                    RaiseLocalEvent(message);
                    return;
                }
            }

            component.Enabled = false;
            message = new GravityChangedEvent(gridId.Value, false);
            RaiseLocalEvent(message);
        }

        private void OnGravityShutdown(EntityUid uid, GravityComponent component, ComponentShutdown args)
        {
            DisableGravity(component);
        }

        public void EnableGravity(GravityComponent comp)
        {
            if (comp.Enabled)
                return;

            var gridId = Transform(comp.Owner).GridUid;
            Dirty(comp);

            if (gridId == null)
                return;

            comp.Enabled = true;
            var message = new GravityChangedEvent(gridId.Value, true);
            RaiseLocalEvent(message);

        }

        public void DisableGravity(GravityComponent comp)
        {
            if (!comp.Enabled)
                return;

            comp.Enabled = false;
            Dirty(comp);

            var gridId = Transform(comp.Owner).GridUid;
            if (gridId == null)
                return;

            var message = new GravityChangedEvent(gridId.Value, false);
            RaiseLocalEvent(message);
        }
    }
}
