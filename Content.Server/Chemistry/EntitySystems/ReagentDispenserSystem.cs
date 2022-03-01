using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Shared.Chemistry.Dispenser;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

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
