using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Mech;

/// <inheritdoc/>
public sealed class MechSystem : SharedMechSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechComponent, AppearanceChangeEvent>(OnAppearanceChanged);
    }

    private void OnAppearanceChanged(EntityUid uid, MechComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.Sprite.TryGetLayer((int) MechVisualLayers.Base, out var layer))
            return;

        var state = component.BaseState;
        if (component.BrokenState != null && args.Component.TryGetData(MechVisuals.Broken, out bool broken) && broken)
            state = component.BrokenState;
        else if (component.OpenState != null && args.Component.TryGetData(MechVisuals.Open, out bool open) && open)
            state = component.OpenState;

        layer.SetState(state);
    }
}
