using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Chemistry.EntitySystems
{
    [UsedImplicitly]
    public class ChemMasterSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, SolutionChangeEvent>(OnSolutionChange);
        }

        private void OnSolutionChange(EntityUid uid, ChemMasterComponent component, SolutionChangeEvent solutionChange)
        {
            component.UpdateUserInterface();
        }
    }
}
