using Content.Shared.Atmos.Piping.Unary.Visuals;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Atmos.Visualizers
{
    [UsedImplicitly]
    public sealed class ScrubberVisualizer : AppearanceVisualizer
    {
        private string _offState = "scrub_off";
        private string _scrubState = "scrub_on";
        private string _siphonState = "scrub_purge";
        private string _weldedState = "scrub_welded";
        private string _wideState = "scrub_wide";

        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(component.Owner, out ISpriteComponent? sprite))
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

    public enum ScrubberVisualLayers : byte
    {
        Scrubber,
    }
}
