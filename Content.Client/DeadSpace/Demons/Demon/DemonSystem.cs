// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Demons.Demon.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.Demons.Demon;

public sealed class DemonSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DemonComponent, GetStatusIconsEvent>(GetDemonIcon);
    }

    private void GetDemonIcon(Entity<DemonComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.FactionIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
