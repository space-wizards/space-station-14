using Content.Shared.GameObjects.Components.Cargo;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    public class GalacticMarketComponent : SharedGalacticMarketComponent
    {
        public override ComponentState GetComponentState()
        {
            return new GalacticMarketState(GetProductIdList());
        }
    }
}
