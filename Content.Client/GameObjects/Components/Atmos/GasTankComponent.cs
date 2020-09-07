using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Client.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class GasTankComponent: Component
    {
        public override string Name => "GasTank";
        public override uint? NetID => ContentNetIDs.GAS_TANK;
    }
}
