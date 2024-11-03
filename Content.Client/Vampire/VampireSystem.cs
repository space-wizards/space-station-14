using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Vampire;

public sealed partial class VampireSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, GetStatusIconsEvent>(GetVampireIcon);
    }
    
    private void GetVampireIcon(Entity<VampireComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<VampireComponent>(ent) &&  _prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}