using Robust.Shared.GameObjects;

namespace Content.Shared.Gravity
{
    public abstract class SharedGravitySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInitialize);
        }

        private void HandleGridInitialize(GridInitializeEvent ev)
        {
            var gridEnt = EntityManager.GetEntity(ev.EntityUid);
            gridEnt.EnsureComponent<GravityComponent>();
        }
    }
}
