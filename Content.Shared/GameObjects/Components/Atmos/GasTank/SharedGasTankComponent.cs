#nullable enable
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Atmos.GasTank
{
    public class SharedGasTankComponent : Component
    {
        public override string Name => "GasTank";
        public override uint? NetID => ContentNetIDs.GAS_TANK;
    }
}
