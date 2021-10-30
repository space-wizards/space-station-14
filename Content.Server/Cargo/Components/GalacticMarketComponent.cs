using Content.Shared.Cargo.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Server.Cargo.Components
{
    [RegisterComponent]
    public class GalacticMarketComponent : SharedGalacticMarketComponent
    {
        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new GalacticMarketState(GetProductIdList());
        }
    }
}
