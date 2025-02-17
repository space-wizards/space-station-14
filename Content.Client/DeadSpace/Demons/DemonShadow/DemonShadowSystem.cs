// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Demons.DemonShadow;
using Content.Shared.DeadSpace.Demons.DemonShadow.Components;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Demons.DemonShadow;

public sealed class DemonShadowSystem : SharedDemonShadowSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DemonShadowComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, DemonShadowComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, DemonShadowVisuals.Astral, out var astral, args.Component) && astral)
        {
            args.Sprite.LayerSetState(0, component.AstralState);
        }
        else if (_appearance.TryGetData<bool>(uid, DemonShadowVisuals.Hide, out var hide, args.Component))
        {
            if (hide)
                args.Sprite.LayerSetState(0, component.HideState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
