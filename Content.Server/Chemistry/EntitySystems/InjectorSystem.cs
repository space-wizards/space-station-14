using Content.Server.Chemistry.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class InjectorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InjectorComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, InjectorComponent component, SolutionChangedEvent args)
        {
            component.Dirty();
        }
    }
}
