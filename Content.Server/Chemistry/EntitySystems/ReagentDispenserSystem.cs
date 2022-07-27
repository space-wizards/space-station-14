using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Emag.Systems;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class ReagentDispenserSystem : SharedReagentDispenserSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionChangedEvent>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<ReagentDispenserComponent, GotEmaggedEvent>(OnEmagged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var components = EntityQuery<ReagentDispenserComponent>();
            foreach (var component in components)
            {
                if(!component.UsesPointLimit) { continue; }
                var nb = component.CurrentPoints += frameTime;
                component.CurrentPoints = nb >= component.MaxPoints ? component.MaxPoints : nb;
                // Can't update the user interface because it clears the selection, which is super annoying.
            }
        }

        private void OnEmagged(EntityUid uid, ReagentDispenserComponent comp, GotEmaggedEvent args)
        {
            if (!comp.AlreadyEmagged)
            {
                comp.AddFromPrototype(comp.EmagPackPrototypeId);
                comp.AlreadyEmagged = true;
                args.Handled = true;
            }
        }
    }
}
