using Content.Shared.VendingMachines;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Threading;

namespace Content.Server.VendingMachines.Restock
{
    [RegisterComponent]
    [Access(typeof(VendingMachineRestockSystem))]
    public sealed class VendingMachineRestockComponent : Component
    {
        [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<VendingMachineInventoryPrototype>))]
        public string PackPrototypeId = string.Empty;

        [DataField("soundRestock")]
        public SoundSpecifier SoundRestock = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/batrifle_magin.ogg");

        [DataField("restockDelay")]
        public float RestockDelay = 5.0f;

        public CancellationTokenSource? CancelToken;
    }
}
