// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Spiders.SpiderLurker;
using Content.Shared.DeadSpace.Spiders.SpiderLurker.Components;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Spiders.SpiderLurker;

public sealed class SpiderLurkerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderLurkerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }
    private void OnAppearanceChange(EntityUid uid, SpiderLurkerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, SpiderLurkerVisuals.hide, out var hide, args.Component))
        {
            if (hide)
                args.Sprite.LayerSetState(0, component.HideState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
