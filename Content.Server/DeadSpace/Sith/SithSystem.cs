// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Humanoid;
using Content.Shared.DeadSpace.Sith.Components;
using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Sith;

namespace Content.Server.DeadSpace.Sith;

public sealed class SithSystem : SharedSithSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SithComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<SithComponent, ComponentShutdown>(OnDown);
    }

    private void OnCompInit(EntityUid uid, SithComponent component, ComponentInit args)
    {
        Timer.Spawn(500,
                    () => SetEyeColor(uid, component));
    }

    private void OnDown(EntityUid uid, SithComponent component, ComponentShutdown args)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var huApComp))
        {
            huApComp.EyeColor = component.OldEyeColor;
        }
    }

    private void SetEyeColor(EntityUid uid, SithComponent component)
    {
        if (TryComp<HumanoidAppearanceComponent>(uid, out var huApComp))
        {
            component.OldEyeColor = huApComp.EyeColor;
            huApComp.EyeColor = component.EyeColor;
        }
    }
}
