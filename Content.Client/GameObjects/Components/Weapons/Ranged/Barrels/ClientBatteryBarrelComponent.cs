using Content.Shared.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Weapons.Ranged.Barrels
{
    [RegisterComponent]
    public sealed class ClientBatteryBarrelComponent : Component
    {
        public override string Name => "BatteryBarrel";
        public override uint? NetID => ContentNetIDs.BATTERY_BARREL;
        
        // TODO: Visualizer
    }
}