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
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}