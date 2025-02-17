// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.DeadSpace.Necromorphs.Unitology;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.Necromorphs.Unitology;

/// <summary>
/// Used for the client to get status icons from other unitologs.
/// </summary>
public sealed class UnitologySystem : SharedUnitologySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyComponent, GetStatusIconsEvent>(GetUniIcon);
        SubscribeLocalEvent<UnitologyHeadComponent, GetStatusIconsEvent>(GetUniHeadIcon);
        SubscribeLocalEvent<UnitologyEnslavedComponent, GetStatusIconsEvent>(GetUniEnslavedIcon);
    }

    private void GetUniIcon(Entity<UnitologyComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<UnitologyHeadComponent>(ent))
            return;

        if (HasComp<UnitologyEnslavedComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetUniHeadIcon(Entity<UnitologyHeadComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<UnitologyEnslavedComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetUniEnslavedIcon(Entity<UnitologyEnslavedComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<UnitologyHeadComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
