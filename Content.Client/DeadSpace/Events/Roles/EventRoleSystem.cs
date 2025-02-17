// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Events.Roles.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.Events.Roles;

public sealed class EventRoleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventRoleComponent, GetStatusIconsEvent>(GetEventIcon);
    }

    private void GetEventIcon(Entity<EventRoleComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.FactionIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
