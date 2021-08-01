using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry;
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

            SubscribeLocalEvent<InjectorComponent, SolutionChangeEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, InjectorComponent component, SolutionChangeEvent args)
        {
            component.Dirty();
        }
    }
}
