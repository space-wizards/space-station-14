// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Store;
using Content.Shared.VendingMachines;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Backmen.Store;

[RegisterComponent, NetworkedComponent]
public sealed partial class BuyStoreBankComponent : Component
{
    /// <summary>
    /// The time (in seconds) that it takes to restock a machine.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("restockDelay")]
    public TimeSpan RestockDelay = TimeSpan.FromSeconds(5.0f);

    /// <summary>
    /// What sort of machine inventory does this restock?
    /// This is checked against the VendingMachineComponent's pack value.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canRestock", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<VendingMachineInventoryPrototype>))]
    public HashSet<string> CanRestock = new();

    /// <summary>
    ///     Sound that plays when starting to restock a machine.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundRestockStart")]
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
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundRestockDone")]
    public SoundSpecifier SoundRestockDone = new SoundPathSpecifier("/Audio/Machines/vending_restock_done.ogg");

    [DataField("emagCategories", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<StoreCategoryPrototype>))]
    public HashSet<string> EmagCategories = new();

    /// <summary>
    /// Emag sound effects.
    /// </summary>
    [DataField("sparkSound")]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8),
    };
}
