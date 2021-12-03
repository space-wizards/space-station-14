using Content.Shared.Gravity;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Gravity.EntitySystems
{
    [UsedImplicitly]
    internal sealed class GravitySystem : SharedGravitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GravityComponent, ComponentInit>(HandleGravityInitialize);
        }

        private void HandleGravityInitialize(EntityUid uid, GravityComponent component, ComponentInit args)
        {
            // Incase there's already a generator on the grid we'll just set it now.
            var gridId = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(component.Owner.Uid).GridID;
            GravityChangedMessage message;

            foreach (var generator in EntityManager.EntityQuery<GravityGeneratorComponent>())
            {
                if (IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(generator.Owner.Uid).GridID == gridId && generator.GravityActive)
                {
                    component.Enabled = true;
                    message = new GravityChangedMessage(gridId, true);
                    RaiseLocalEvent(message);
                    return;
                }
            }

            component.Enabled = false;
            message = new GravityChangedMessage(gridId, false);
            RaiseLocalEvent(message);
        }

        public void EnableGravity(GravityComponent comp)
        {
            if (comp.Enabled) return;
            comp.Enabled = true;

            var gridId = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(comp.Owner.Uid).GridID;
            var message = new GravityChangedMessage(gridId, true);
            RaiseLocalEvent(message);
        }

        public void DisableGravity(GravityComponent comp)
        {
            if (!comp.Enabled) return;
            comp.Enabled = false;

            var gridId = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(comp.Owner.Uid).GridID;
            var message = new GravityChangedMessage(gridId, false);
            RaiseLocalEvent(message);
        }
    }
}
