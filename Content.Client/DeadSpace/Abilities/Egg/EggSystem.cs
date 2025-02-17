// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Abilities.Egg.Components;
using Content.Shared.DeadSpace.Abilities.Egg;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.Abilities.Egg;

/// <summary>
/// Used for the client to get status icons from other unitologs.
/// </summary>
public sealed class EggSystem : SharedEggSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggComponent, GetStatusIconsEvent>(GetEggIcon);
    }

    private void GetEggIcon(Entity<EggComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
