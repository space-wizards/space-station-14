using Content.Shared.Cargo.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Server.Cargo.Components
{
    [RegisterComponent]
    public sealed class GalacticMarketComponent : SharedGalacticMarketComponent
    {
        public override ComponentState GetComponentState()
        {
            return new GalacticMarketState(GetProductIdList());
        }
    }
}
