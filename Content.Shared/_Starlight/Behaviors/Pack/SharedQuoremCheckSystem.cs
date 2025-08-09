using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;


namespace Content.Shared._Starlight.Behaviors.Pack;

/// <summary>
/// This handles deciding whether the pack has reached critical mass
/// </summary>
public abstract class SharedQuoremCheckSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected Dictionary<int, HashSet<EntityUid>> _packGroups = new Dictionary<int, HashSet<EntityUid>>();
    private int _nextId;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<QuoremCheckComponent, StartCollideEvent>(OnStartCollide);
        // SubscribeLocalEvent<QuoremCheckComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<QuoremCheckComponent, MobStateChangedEvent>(OnPackmateDeath);
        SubscribeLocalEvent<QuoremCheckComponent, ComponentInit>(OnComponentInit);
        
    }

    // Provide individuals with pack ids
    private void OnComponentInit(Entity<QuoremCheckComponent> ent, ref ComponentInit args)
    {
       ent.Comp.PackId = _nextId;
       _packGroups.Add(_nextId, new HashSet<EntityUid>(){ent});
       _nextId ++;
    }

    // Form packs when individuals become close enough
    private void OnStartCollide(Entity<QuoremCheckComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != "fix2") 
            return;
        
        if (!TryComp(args.OtherEntity, out QuoremCheckComponent? othercomp))
            return;

        if (ent.Comp.PackTag != othercomp.PackTag)
            return;

        if (_mobState.IsDead(ent) || _mobState.IsDead(args.OtherEntity))
            return;

        if (ent.Comp.PackId <= othercomp.PackId)
            return;

        if (!_packGroups.ContainsKey(ent.Comp.PackId) || !_packGroups.ContainsKey(othercomp.PackId))
            return;
        
        // Remove this pack from the list of packs
        _packGroups.Remove(ent.Comp.PackId, out var pack);
        
        pack ??= new HashSet<EntityUid>();
        
        // Combine packs
        _packGroups[othercomp.PackId].UnionWith(pack);
            
        // How many members are in the pack
        var packSize = _packGroups[othercomp.PackId].Count;
        
        // Update other pack members
        foreach (var uid in _packGroups[othercomp.PackId])
        {
            UpdateMembership(uid, othercomp.PackId, packSize);
            
        }
        
    }

    // Update an individual's membership when they enter a new pack
    private void UpdateMembership(EntityUid uid, int newPackId, int packSize)
    {
        if(!TryComp(uid, out QuoremCheckComponent? comp))
            return;
        comp.PackId = newPackId;
        UpdateHostile((uid, comp), packSize);
        
    }
    
    // Remove a pack member on death 
    private void OnPackmateDeath(Entity<QuoremCheckComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;
        
        if (!_packGroups.TryGetValue(ent.Comp.PackId, out var pack))
            return;
        pack.Remove(ent);
        ent.Comp.PackId = _nextId;
        _nextId++;
        
        // If there are now 0 members in the pack remove it
        if(pack.Count == 0)
            _packGroups.Remove(ent.Comp.PackId);

    }
    
    // Change faction to a hostile faction
    private void MakeHostile(Entity<QuoremCheckComponent> ent)
    {
        ent.Comp.IsHostile = true;
        _npcFactionSystem.ClearFactions((ent, null));
        _npcFactionSystem.AddFaction((ent, null), ent.Comp.QuoremFaction);
        
        var coords = new EntityCoordinates(ent.Owner, new Vector2(0, 1));
        SpawnAttachedTo(ent.Comp.QuoremEffect, coords);
        _audio.PlayPvs(ent.Comp.QuoremSound, ent.Owner);
 
    }
    
    // Check to see if the Quorem has been reached. If so become hostile
    public void UpdateHostile(Entity<QuoremCheckComponent> ent, int packSize)
    {
        if (packSize >= ent.Comp.QuoremThreshold)
        {
            MakeHostile(ent);
        }

    }
    
    
}