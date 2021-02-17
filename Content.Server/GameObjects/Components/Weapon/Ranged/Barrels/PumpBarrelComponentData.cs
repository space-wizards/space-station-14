using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    public partial class PumpBarrelComponentData
    {
        [DataField("capacity")] [DataClassTarget("capacity")]
        public int Capacity = 6;
    }
}
