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

    private List<Entity<LightningTargetComponent>> _lookupTargetsList = new();
    private HashSet<Entity<LightningTargetComponent>> _lookupTargets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningComponent, ComponentRemove>(OnRemove);
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
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(user, target, lightningPrototype, spriteState);

        var ev = new HitByLightningEvent(user, target);
        RaiseLocalEvent(target, ref ev);
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
        //To Do: Remove Hardcode LightningTargetComponent (this should be a parameter of the SharedLightningComponent)
        //To Do: This is still pretty bad for perf but better than before and at least it doesn't re-allocate
        // several hashsets every time

        var targets = _lookup.GetComponentsInRange<LightningTargetComponent>(Transform(user).MapPosition, range).ToList();
        _random.Shuffle(targets);
        targets.Sort((x, y) => y.Priority.CompareTo(x.Priority));
        //var realCount = Math.Min(targets.Count, boltCount);

        int shootedCount = 0;
        int count = -1;
        while(shootedCount < boltCount)
        {
            count++;

            if (count >= targets.Count) { break; }

            var curTarget = targets[count];
            if (!_random.Prob(curTarget.HitProbability)) //Chance to ignore target
                continue;

            ShootLightning(user, targets[count].Owner, lightningPrototype);
            if (arcDepth - targets[count].LightningResistance > 0)
            {
                ShootRandomLightnings(targets[count].Owner, range, 1, lightningPrototype, arcDepth - targets[count].LightningResistance);
            }
            shootedCount++;
        }
    }
}

/// <summary>
/// Raised directed on the target when an entity becomes the target of a lightning strike (not when touched)
/// </summary>
/// <param name="Source">The entity that created the lightning</param>
/// <param name="Target">The entity that was struck by lightning.</param>
[ByRefEvent]
public readonly record struct HitByLightningEvent(EntityUid Source, EntityUid Target);
