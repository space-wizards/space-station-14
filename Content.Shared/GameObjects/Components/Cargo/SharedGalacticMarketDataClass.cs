using System.Collections.Generic;
using Content.Shared.Prototypes.Cargo;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Cargo
{
    public partial class SharedGalacticMarketDataClass
    {
        // TODO PAUL SERV3
        [DataField("products")] [DataClassTarget("products")]
        protected List<CargoProductPrototype> _products = new();
    }
}
