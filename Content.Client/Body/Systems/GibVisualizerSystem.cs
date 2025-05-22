using Content.Shared.Body.Organ;
using Content.Server.Body.Systems;
using Robust.Client.GameObjects;

using Content.Shared.Hands;

namespace Content.Client.Body.Systems;

/// <summary>
/// Ensures entities with <see cref="OrganComponent"/> have the correct color.
/// </summary>
public sealed class GibVisualizerSystem : VisualizerSystem<OrganComponent>
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
    }

    private void OnHeldVisualsUpdated(EntityUid uid, OrganComponent component, HeldVisualsUpdatedEvent args)
    {
        if (!TryComp<SpriteComponent>(args.User, out var sprite) || !AppearanceSystem.TryGetData<Color>(uid, GoreVisuals.ColorTint, out var color))
            return;
        foreach (string layer in args.RevealedLayers)
        {
            var index = _spriteSystem.LayerMapReserve((args.User, sprite), layer);
            _spriteSystem.LayerSetColor((args.User, sprite), index, color);
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, OrganComponent comp, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<Color>(uid, GoreVisuals.ColorTint, out var color))
            return;

        _spriteSystem.SetColor(uid, color);
    }
}
