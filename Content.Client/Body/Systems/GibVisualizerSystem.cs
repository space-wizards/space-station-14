using Content.Shared.Body.Organ;
using Content.Server.Body.Systems;
using Robust.Client.GameObjects;

using Content.Shared.Item;

namespace Content.Client.Body;

/// <summary>
/// Ensures entities with <see cref="OrganComponent"/> have the correct color.
/// </summary>
public sealed class GibVisualizerSystem : VisualizerSystem<OrganComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, VisualsChangedEvent>(OnVisualsChanged);
    }

    // TODO: This is supposed to callback when an item is held but it doesn't?
    // Want to tint held slime organs too.
    private void OnVisualsChanged(EntityUid uid, OrganComponent component, VisualsChangedEvent args)
    {
        var item = GetEntity(args.Item);
    }

    protected override void OnAppearanceChange(EntityUid uid, OrganComponent comp, ref AppearanceChangeEvent args)
    {
        if (AppearanceSystem.TryGetData<Color>(uid, GoreVisuals.ColorTint, out var color))
        {
            if (null != args.Sprite)
            {
                args.Sprite.Color = color;
                return;
            }
        }
    }
}
