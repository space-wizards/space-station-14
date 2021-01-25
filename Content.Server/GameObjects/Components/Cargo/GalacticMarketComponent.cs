using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Cargo;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Cargo
{
    [RegisterComponent]
    public class GalacticMarketComponent : SharedGalacticMarketComponent
    {
        private List<CargoTraderProfile> _traderProfiles = new();
        public IReadOnlyList<CargoTraderProfile> TraderProfiles => _traderProfiles;
        public override ComponentState GetComponentState()
        {
            return new GalacticMarketState(GetProductIdList());
        }
    }
}
