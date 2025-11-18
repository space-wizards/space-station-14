using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedVendingMachineSystem))]
public sealed partial class VendingMachineRestockComponent : Component
{
    /// <summary>
    /// The time (in seconds) that it takes to restock a machine.
    /// </summary>
    [DataField]
    public TimeSpan RestockDelay = TimeSpan.FromSeconds(5.0f);

    /// <summary>
    /// What sort of machine inventory does this restock?
    /// This is checked against the VendingMachineComponent's pack value.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<VendingMachineInventoryPrototype>))]
    public HashSet<string> CanRestock = [];

    /// <summary>
    ///     Sound that plays when starting to restock a machine.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundRestockStart = new SoundPathSpecifier("/Audio/Machines/vending_restock_start.ogg")
    {
        Params = new AudioParams
        {
            Volume = -2f,
            Variation = 0.2f
        }
    };

    /// <summary>
    ///     Sound that plays when finished restocking a machine.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundRestockDone = new SoundPathSpecifier("/Audio/Machines/vending_restock_done.ogg",
        AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));
}

[Serializable, NetSerializable]
public sealed partial class RestockDoAfterEvent : SimpleDoAfterEvent;
