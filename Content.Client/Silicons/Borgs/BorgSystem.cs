using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Silicons.Borgs;

/// <inheritdoc/>
public sealed class BorgSystem : SharedBorgSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MMIComponent, AppearanceChangeEvent>(OnMMIAppearanceChanged);
    }

    private void OnMMIAppearanceChanged(EntityUid uid, MMIComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        var sprite = args.Sprite;

        if (!_appearance.TryGetData(uid, MMIVisuals.BrainPresent, out bool brain))
            brain = false;
        if (!_appearance.TryGetData(uid, MMIVisuals.HasMind, out bool hasMind))
            hasMind = false;

        sprite.LayerSetVisible(MMIVisualLayers.Brain, brain);
        if (!brain)
        {
            sprite.LayerSetState(MMIVisualLayers.Base, component.NoBrainState);
        }
        else
        {
            var state = hasMind
                ? component.HasMindState
                : component.NoMindState;
            sprite.LayerSetState(MMIVisualLayers.Base, state);
        }
    }
}
