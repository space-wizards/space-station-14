using System.Threading;
using Robust.Shared.Audio;

namespace Content.Server.VendingMachineRestockPackage
{
    [RegisterComponent]
    public sealed class VendingMachineRestockPackageComponent : Component
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
        // Credit to: https://freesound.org/people/Defaultv/sounds/534362/
        public SoundSpecifier SoundRestockStart = new SoundPathSpecifier("/Audio/Machines/vending_restock_start.ogg");

        /// <summary>
        ///     Sound that plays when finished restocking a machine.
        /// </summary>
        [DataField("soundRestockDone")]
        // Credit to: https://freesound.org/people/felipelnv/sounds/153298/
        public SoundSpecifier SoundRestockDone = new SoundPathSpecifier("/Audio/Machines/vending_restock_done.ogg");
    }
}
