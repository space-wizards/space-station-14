// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Demons.Herald;
using Content.Shared.DeadSpace.Demons.Herald.Components;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Demons.Herald;

public sealed class HeraldSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeraldComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, HeraldComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, HeraldVisuals.Enraged, out var enraging, args.Component))
        {
            if (enraging)
                args.Sprite.LayerSetState(0, component.EnragingState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
