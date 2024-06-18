using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power;

namespace Content.Server.Power.Components
{
    [RegisterComponent]
    public sealed partial class ChargingComponent : Component
    {
        ///<summary>
        ///References the entity of the charger that is currently powering this battery
        ///</summary>
        public EntityUid ChargerUid;

        ///<summary>
        ///References the component of the charger that is currently powering this battery
        ///</summary>
        public ChargerComponent ChargerComponent;
    }
}
