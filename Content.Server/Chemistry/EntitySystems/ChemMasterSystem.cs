using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public sealed class ChemMasterSystem : SharedChemMasterSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<ChemMasterComponent, SolutionChangedEvent>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>((_, comp, _) => comp.UpdateUserInterface());
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>((_, comp, _) => comp.UpdateUserInterface());
        }
    }
}
