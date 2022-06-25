using Content.Client.Atmos.Components;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Robust.Client.GameObjects;

namespace Content.Client.Atmos.EntitySystems;

/// <summary>
/// This handles...
/// </summary>
public sealed class ScrubberVisualizerSystem : VisualizerSystem<ScrubberVisualsComponent>
{
    private string _offState = "scrub_off";
    private string _scrubState = "scrub_on";
    private string _siphonState = "scrub_purge";
    private string _weldedState = "scrub_welded";
    private string _wideState = "scrub_wide";

    protected override void OnAppearanceChange(EntityUid uid, ScrubberVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!args.Component.TryGetData<ScrubberState>(ScrubberVisuals.State, out var state))
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
            default:
                sprite.LayerSetState(ScrubberVisualLayers.Scrubber, _offState);
                break;
        }
    }
}

public enum ScrubberVisualLayers : byte
{
    Scrubber,
}
