using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Atmos.GasTank
{
    public abstract class SharedGasTankComponent : Component
    {
        public override uint? NetID => ContentNetIDs.GAS_TANK;
    }
}
