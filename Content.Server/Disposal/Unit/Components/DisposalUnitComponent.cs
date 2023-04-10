using System.Threading;
using Content.Server.Atmos;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;
using Robust.Shared.Containers;

namespace Content.Server.Disposal.Unit.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public sealed class DisposalUnitComponent : SharedDisposalUnitComponent, IGasMixtureHolder
    {
        /// <summary>
        ///     Last time that an entity tried to exit this disposal unit.
        /// </summary>
        [ViewVariables]
        public TimeSpan LastExitAttempt;

        /// <summary>
        ///     The current pressure of this disposal unit.
        ///     Prevents it from flushing if it is not equal to or bigger than 1.
        /// </summary>
        [DataField("pressure")]
        public float Pressure = 1f;

        [DataField("autoEngageEnabled")]
        public bool AutomaticEngage = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("autoEngageTime")]
        public readonly TimeSpan AutomaticEngageTime = TimeSpan.FromSeconds(30);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("flushDelay")]
        public readonly TimeSpan FlushDelay = TimeSpan.FromSeconds(3);

        /// <summary>
        ///     Delay from trying to enter disposals ourselves.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("entryDelay")]
        public float EntryDelay = 0.5f;

        /// <summary>
        ///     Delay from trying to shove someone else into disposals.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float DraggedEntryDelay = 0.5f;

        /// <summary>
        ///     Token used to cancel the automatic engage of a disposal unit
        ///     after an entity enters it.
        /// </summary>
        public CancellationTokenSource? AutomaticEngageToken;

        /// <summary>
        ///     Container of entities inside this disposal unit.
        /// </summary>
        [ViewVariables] public Container Container = default!;

        [ViewVariables] public bool Powered = false;

        [ViewVariables] public PressureState State = PressureState.Ready;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Engaged { get; set; }

        [DataField("air")]
        public GasMixture Air { get; set; } = new(Atmospherics.CellVolume);
    }
}
