using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Ensnaring;

public sealed class EnsnareableSystem : SharedEnsnareableSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    protected override void OnEnsnareInit(Entity<EnsnareableComponent> ent, ref ComponentInit args)
    {
        base.OnEnsnareInit(ent, ref args);

        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        // TODO remove this, this should just be in yaml.
        _sprite.LayerMapReserve((ent.Owner, sprite), EnsnaredVisualLayers.Ensnared);
    }

    private void OnAppearanceChange(EntityUid uid, EnsnareableComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !_sprite.LayerMapTryGet((uid, args.Sprite), EnsnaredVisualLayers.Ensnared, out var layer, false))
            return;

        if (_appearance.TryGetData<bool>(uid, EnsnareableVisuals.IsEnsnared, out var isEnsnared, args.Component))
        {
            if (component.Sprite != null)
            {
                _sprite.LayerSetRsi((uid, args.Sprite), layer, new ResPath(component.Sprite));
                _sprite.LayerSetRsiState((uid, args.Sprite), layer, component.State);
                _sprite.LayerSetVisible((uid, args.Sprite), layer, isEnsnared);
            }
        }
    }
}

public enum EnsnaredVisualLayers : byte
{
    Ensnared,
}
