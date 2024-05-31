using Content.Shared.Flash;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Client.Flash
{
    public sealed class FlashSystem : SharedFlashSystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FlashableComponent, ComponentHandleState>(OnFlashableHandleState);
        }

        private void OnFlashableHandleState(EntityUid uid, FlashableComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not FlashableComponentState state)
                return;

            // Yes, this code is awful. I'm just porting it to an entity system so don't blame me.
            if (_playerManager.LocalEntity != uid)
            {
                return;
            }

            if (state.Time == default)
            {
                return;
            }

            // Few things here:
            // 1. If a shorter duration flash is applied then don't do anything
            // 2. If the client-side time is later than when the flash should've ended don't do anything
            var currentTime = _gameTiming.CurTime.TotalSeconds;
            var newEndTime = state.Time.TotalSeconds + state.Duration;
            var currentEndTime = component.LastFlash.TotalSeconds + component.Duration;

            if (currentEndTime > newEndTime)
            {
                return;
            }

            if (currentTime > newEndTime)
            {
                return;
            }

            component.LastFlash = state.Time;
            component.Duration = state.Duration;

            var overlay = _overlayManager.GetOverlay<FlashOverlay>();
            overlay.ReceiveFlash(component.Duration);
        }
    }
}
