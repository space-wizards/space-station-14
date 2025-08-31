using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared._Starlight.Behaviors.Pack;

namespace Content.Server._Starlight.Behaviors.Pack;

public sealed class QuoremCheckSystem : SharedQuoremCheckSystem
{
    [Dependency] private readonly NPCRetaliationSystem _retaliation = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<QuoremCheckComponent, AfterRetaliationEvent>(OnAfterRetaliation);
        
    }
    private void OnAfterRetaliation(Entity<QuoremCheckComponent> ent, ref AfterRetaliationEvent args)
    { 
        if(!_packGroups.TryGetValue(ent.Comp.PackId, out var packGroup))
            return;

        foreach (var uid in packGroup)
        {
            if (uid == ent.Owner) 
                continue;

            if (!TryComp(uid, out NPCRetaliationComponent? retaliation))
                continue;
            
            // Other pack members will retaliate, but not raise events
            _retaliation.TryRetaliate((uid, retaliation), args.Origin, raiseEvent: false);
            
        }
    }

    
}