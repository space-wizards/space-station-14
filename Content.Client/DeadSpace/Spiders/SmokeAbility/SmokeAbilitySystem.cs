// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Spiders.SmokeAbility;
using Content.Shared.DeadSpace.Spiders.SmokeAbility.Components;
using Robust.Client.GameObjects;

namespace Content.Client.DeadSpace.Spiders.SmokeAbility;

public sealed class SmokeAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokeAbilityComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }
    private void OnAppearanceChange(EntityUid uid, SmokeAbilityComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, SmokeAbilityVisuals.hide, out var hide, args.Component))
        {
            if (hide)
                args.Sprite.LayerSetState(0, component.HideState);
            else
                args.Sprite.LayerSetState(0, component.State);
        }
    }
}
