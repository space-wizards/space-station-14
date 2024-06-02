using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class WeldingMaskComponent : Component
    {
        public EntityUid Equipee;
    }
}
