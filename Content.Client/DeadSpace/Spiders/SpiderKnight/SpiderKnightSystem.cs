// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Spiders.SpiderKnight;
using Content.Shared.DeadSpace.Spiders.SpiderKnight.Components;
using Robust.Client.GameObjects;
using Content.Shared.Examine;

namespace Content.Client.DeadSpace.Spiders.SpiderKnight;

public sealed class SpiderKnightSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderKnightComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, SpiderKnightComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, SpiderKnightVisuals.defend, out var defend, args.Component) && defend)
        {
            args.Sprite.LayerSetState(0, component.DefendState);
        }
        else if (_appearance.TryGetData<bool>(uid, SpiderKnightVisuals.attack, out var attack, args.Component))
        {
            if (attack)
                args.Sprite.LayerSetState(0, component.AttackState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
