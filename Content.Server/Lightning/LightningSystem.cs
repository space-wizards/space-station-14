using System.Linq;
using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Server.Lightning.Components;
using Content.Shared.Lightning;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.Lightning;

// TheShuEd:
//I've redesigned the lightning system to be more optimized.
//Previously, each lightning element, when it touched something, would try to branch into nearby entities.
//So if a lightning bolt was 20 entities long, each one would check its surroundings and have a chance to create additional lightning...
//which could lead to recursive creation of more and more lightning bolts and checks.

//I redesigned so that lightning branches can only be created from the point where the lightning struck, no more collide checks
//and the number of these branches is explicitly controlled in the new function.
public sealed class LightningSystem : SharedLightningSystem
{
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private Dictionary<int, HashSet<Entity<LightningTargetComponent>>> _allTargets = new Dictionary<int, HashSet<Entity<LightningTargetComponent>>>();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<LightningTargetComponent, MapInitEvent>(OnTargetInit);
        SubscribeLocalEvent<LightningTargetComponent, ComponentRemove>(OnTargetRemove);
    }

    private void OnTargetInit(Entity<LightningTargetComponent> target, ref MapInitEvent args)
    {
        var priority = target.Comp.Priority;
        if (!_allTargets.ContainsKey(priority))
            _allTargets.Add(priority, new HashSet<Entity<LightningTargetComponent>>());

        _allTargets[priority].Add(new Entity<LightningTargetComponent>(target.Owner, target.Comp));
        Log.Debug($"Added {target}, priority = {priority}, count = {_allTargets[priority].Count}");
    }

    private void OnTargetRemove(Entity<LightningTargetComponent> target, ref ComponentRemove args)
    {
        var priority = target.Comp.Priority;

        _allTargets[priority].Remove(target);

        Log.Debug($"Remove {target}, priority = {priority}, count = {_allTargets[priority].Count}");
    }

    private void OnRemove(EntityUid uid, LightningComponent component, ComponentRemove args)
    {
        if (!TryComp<BeamComponent>(uid, out var lightningBeam) || !TryComp<BeamComponent>(lightningBeam.VirtualBeamController, out var beamController))
        {
            return;
        }

        beamController.CreatedBeams.Remove(uid);
    }

    /// <summary>
    /// Fires lightning from user to target
    /// </summary>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="target">Where the lightning fires to</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    public void ShootLightning(EntityUid user, EntityUid target, string lightningPrototype = "Lightning")
    {
        if (Deleted(user) || Deleted(target))
            return;

        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(user, target, lightningPrototype, spriteState);

        var ev = new HitByLightningEvent(user, target);
        RaiseLocalEvent(target, ref ev, true);
    }

    /// <summary>
    /// Looks for objects with a LightningTarget component in the radius, prioritizes them, and hits the highest priority targets with lightning.
    /// </summary>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="range">Targets selection radius</param>
    /// <param name="boltCount">Number of lightning bolts</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="arcDepth">how many times to recursively fire lightning bolts from the target points of the first shot.</param>
    public void ShootRandomLightnings(EntityUid user, float range, int boltCount, string lightningPrototype = "Lightning", int arcDepth = 0)
    {
        //To Do: add support to different priority target tablem for different lightning types

        //var boltRemains = boltCount;
        //var targetPriority = _allTargets.Keys.Max();
        //
        //var hashSet = _allTargets[targetPriority];
        //_lookup.GetEntitiesInRange(Transform(user).MapPosition, range, hashSet);
        //
        //while (boltRemains > 0)
        //{
        //    //Move to lower priority
        //    if (hashSet.Count == 0) //Move to next priority
        //    {
        //        targetPriority--;
        //        hashSet = _allTargets[targetPriority];
        //        _lookup.GetEntitiesInRange(Transform(user).MapPosition, range, hashSet);
        //        continue;
        //    }
        //}
        //
        //for (int i = 0; i < boltCount; i++)
        //{
        //    //найти цель
        //
        //    if (hashSet.Count == 0) //Move to next priority
        //    {
        //        targetPriority--;
        //        i++;
        //        continue;
        //    }
        //    //выстрелить
        //
        //
        //
        //}
        //
        //while (boltRemains > 0)
        //{
        //    var hashSet = _allTargets[targetPriority];
        //    _lookup.GetEntitiesInRange(Transform(user).MapPosition, range, hashSet);
        //    for (int i = 0; i < boltRemains; i++)
        //    {
        //
        //    }
        //}
        //

        //var targets = _lookup.GetComponentsInRange<LightningTargetComponent>(Transform(user).MapPosition, range).ToList();
        //_random.Shuffle(targets);
        //targets.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        //
        //var realCount = Math.Min(targets.Count, boltCount);
        //
        //if (realCount <= 0)
        //    return;
        //
        //for (int i = 0; i < realCount; i++)
        //{
        //    ShootLightning(user, targets[i].Owner, lightningPrototype); //idk how to evade .Owner pls help
        //
        //    if (arcDepth > 0)
        //    {
        //        ShootRandomLightnings(targets[i].Owner, range, 1, lightningPrototype, arcDepth - targets[i].LightningResistance);
        //    }
        //}
    }
}

/// <summary>
/// Invoked when an entity becomes the target of a lightning strike (not when touched)
/// </summary>
/// <param name="Source">The entity that created the lightning</param>
/// <param name="Target">The entity that was struck by lightning.</param>
[ByRefEvent]
public readonly record struct HitByLightningEvent(EntityUid Source, EntityUid Target);
