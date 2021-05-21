using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public abstract class SharedStackSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedStackComponent, ComponentStartup>(OnStackStarted);
            SubscribeLocalEvent<SharedStackComponent, StackCountChangedEvent>(OnStackCountChanged);
        }

        private void OnStackStarted(EntityUid uid, SharedStackComponent component, ComponentStartup args)
        {
            if (!ComponentManager.TryGetComponent(uid, out SharedAppearanceComponent? appearance))
                return;

            appearance.SetData(StackVisuals.MaxCount, component.MaxCount);
            appearance.SetData(StackVisuals.Hide, false);
        }

        protected virtual void OnStackCountChanged(EntityUid uid, SharedStackComponent component, StackCountChangedEvent args)
        {
            // Delete stack if count reaches zero.
            if(args.NewCount <= 0)
                EntityManager.QueueDeleteEntity(uid);

            // Change appearance data.
            if (ComponentManager.TryGetComponent(uid, out SharedAppearanceComponent? appearance))
                appearance.SetData(StackVisuals.Actual, args.NewCount);
        }
    }
}
