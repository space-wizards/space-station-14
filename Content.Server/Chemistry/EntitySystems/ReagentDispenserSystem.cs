using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class ReagentDispenserSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionChangedEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, ReagentDispenserComponent component, SolutionChangedEvent args)
        {
            component.UpdateUserInterface();
        }
    }
}
