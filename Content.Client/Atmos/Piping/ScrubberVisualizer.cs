using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Atmos.Visuals;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.Piping
{
    [UsedImplicitly]
    public class ScrubberVisualizer : AppearanceVisualizer
    {
        private string _offState = "scrub_off";
        private string _scrubState = "scrub_on";
        private string _siphonState = "scrub_purge";
        private string _weldedState = "scrub_welded";
        private string _wideState = "scrub_wide";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out ISpriteComponent? sprite))
                return;

            if (!component.TryGetData(ScrubberVisuals.State, out ScrubberState state))
                return;

            switch (state)
            {
                case ScrubberState.Off:
                    sprite.LayerSetState(ScrubberVisualLayers.Scrubber, _offState);
                    break;
                case ScrubberState.Scrub:
                    sprite.LayerSetState(ScrubberVisualLayers.Scrubber, _scrubState);
                    break;
                case ScrubberState.Siphon:
                    sprite.LayerSetState(ScrubberVisualLayers.Scrubber, _siphonState);
                    break;
                case ScrubberState.Welded:
                    sprite.LayerSetState(ScrubberVisualLayers.Scrubber, _weldedState);
                    break;
                case ScrubberState.WideScrub:
                    sprite.LayerSetState(ScrubberVisualLayers.Scrubber, _wideState);
                    break;
            }
        }
    }

    public enum ScrubberVisualLayers
    {
        Scrubber,
    }
}
