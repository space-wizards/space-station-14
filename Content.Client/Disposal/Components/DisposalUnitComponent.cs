using Content.Shared.Disposal.Components;
using Robust.Client.Animations;
using Robust.Shared.Audio;

namespace Content.Client.Disposal.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public sealed class DisposalUnitComponent : SharedDisposalUnitComponent
    {
        [DataField("state_anchored", required: true)]
        public readonly string? StateAnchored;

        [DataField("state_unanchored", required: true)]
        public readonly string? StateUnAnchored;

        [DataField("state_charging", required: true)]
        public readonly string? StateCharging;

        [DataField("overlay_charging", required: true)]
        public readonly string? OverlayCharging;

        [DataField("overlay_ready", required: true)]
        public readonly string? OverlayReady;

        [DataField("overlay_full", required: true)]
        public readonly string? OverlayFull;

        [DataField("overlay_engaged", required: true)]
        public readonly string? OverlayEngaged;

        [DataField("state_flush", required: true)]
        public readonly string? StateFlush;

        [DataField("flush_sound", required: true)]
        public readonly SoundSpecifier FlushSound = default!;

        [DataField("flush_time", required: true)]
        public readonly float FlushTime;

        public Animation FlushAnimation = default!;

        public DisposalUnitBoundUserInterfaceState? UiState;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not DisposalUnitComponentState state) return;

            RecentlyEjected = state.RecentlyEjected;
        }
    }
}
