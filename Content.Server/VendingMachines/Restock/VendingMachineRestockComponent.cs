using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.VendingMachines.Restock
{
    [RegisterComponent]
    public sealed class VendingMachineRestockComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// The time (in seconds) that it takes to restock a machine.
        /// </summary>
        [DataField("restockDelay")]
        public float RestockDelay = 8.0f;

        /// <summary>
        /// What sort of machine inventory does this restock?
        /// This is checked against the VendingMachineComponent's pack value.
        /// </summary>
        [DataField("canRestock")]
        public HashSet<string> CanRestock = new();

        /// <summary>
        ///     Sound that plays when starting to restock a machine.
        /// </summary>
        [DataField("soundRestockStart")]
        public SoundSpecifier SoundRestockStart = new SoundPathSpecifier("/Audio/Machines/vending_restock_start.ogg");

        /// <summary>
        ///     Sound that plays when finished restocking a machine.
        /// </summary>
        [DataField("soundRestockDone")]
        public SoundSpecifier SoundRestockDone = new SoundPathSpecifier("/Audio/Machines/vending_restock_done.ogg");
    }
}
