// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Renegade.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.Renegade;

public sealed class RenegadeSubordinateSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RenegadeSubordinateComponent, GetStatusIconsEvent>(GetSubordinateIcon);
    }

    private void GetSubordinateIcon(Entity<RenegadeSubordinateComponent> ent, ref GetStatusIconsEvent args)
    {
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}
