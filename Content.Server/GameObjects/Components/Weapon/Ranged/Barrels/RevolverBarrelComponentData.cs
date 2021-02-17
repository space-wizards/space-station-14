#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    public partial class RevolverBarrelComponentData : ISerializationHooks
    {
        [DataField("capacity")] private int? _capacity = 6;

        [DataClassTarget("ammoSlots")]
        public IEntity[]? AmmoSlots;

        public void BeforeSerialization()
        {
            _capacity = AmmoSlots?.Length;
        }

        public void AfterDeserialization()
        {
            AmmoSlots = _capacity != null ? new IEntity[_capacity.Value] : null;
        }
    }
}
