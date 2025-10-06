// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Humanoid;
using Content.Shared.DeadSpace.Renegade.Components;
using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Renegade;

namespace Content.Server.DeadSpace.Renegade;

public sealed class RenegadeSystem : SharedRenegadeSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenegadeComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<RenegadeComponent, ComponentShutdown>(OnDown);
    }

    private void OnCompInit(EntityUid uid, RenegadeComponent component, ComponentInit args)
    {
        Timer.Spawn(500,
                    () => SetEyeColor(uid, component));
    }

    private void OnDown(EntityUid uid, RenegadeComponent component, ComponentShutdown args)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var huApComp))
        {
            huApComp.EyeColor = component.OldEyeColor;
        }
    }

    private void SetEyeColor(EntityUid uid, RenegadeComponent component)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var huApComp))
        {
            component.OldEyeColor = huApComp.EyeColor;
            huApComp.EyeColor = component.EyeColor;
        }
    }
}
