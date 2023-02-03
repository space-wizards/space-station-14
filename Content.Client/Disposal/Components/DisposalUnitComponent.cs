using Content.Shared.Disposal.Components;
using Robust.Client.Animations;
using Robust.Shared.Audio;

namespace Content.Client.Disposal.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedDisposalUnitComponent))]
    public sealed class DisposalUnitComponent : SharedDisposalUnitComponent
    {
        [DataField("flushSound")]
        public readonly SoundSpecifier? FlushSound;

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
