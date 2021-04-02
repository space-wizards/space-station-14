using Content.Shared.GameObjects.Components.Cargo;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.Components.Cargo
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
