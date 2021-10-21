using Content.Server.Construction.Components;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Server.Construction
{
    public partial class ConstructionSystem
    {
        private void InitializeSteps()
        {
            SubscribeLocalEvent<ConstructionComponent, InteractUsingEvent>(OnConstructionInteractUsing);
        }

        private void OnConstructionInteractUsing(EntityUid uid, ConstructionComponent construction, InteractUsingEvent args)
        {

        }
    }
}
