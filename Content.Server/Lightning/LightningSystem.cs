using System.Linq;
using Content.Server.Beam;
using Content.Server.Beam.Components;
using Content.Server.Lightning.Components;
using Content.Server.Lightning.Events;
using Content.Shared.Lightning;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server.Lightning;

public sealed class LightningSystem : SharedLightningSystem
{
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

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
        if (Deleted(user) || Deleted(target))
            return;

        Log.Debug("ПИУ");
        var spriteState = LightningRandomizer();
        _beam.TryCreateBeam(user, target, lightningPrototype, spriteState);
        Log.Debug("Пиу получился");
        var ev = new HittedByLightningEvent(user, target);
        RaiseLocalEvent(target, ref ev, true);
        Log.Debug("Пиу отправил ивент");
    }

    /// <summary>
    /// Fires lightning bolts at random targets in a radius.
    /// </summary>
    /// <param name="user">Where the lightning fires from</param>
    /// <param name="range">Targets selection radius</param>
    /// <param name="boltCount">Number of lightning bolts</param>
    /// <param name="lightningPrototype">The prototype for the lightning to be created</param>
    /// <param name="arcDepth">how many times to recursively fire lightning bolts from the target points of the first shot.</param>
    public void ShootRandomLightnings(EntityUid user, float range, int boltCount, string lightningPrototype = "Lightning", int arcDepth = 0)
    {
        Log.Debug("Внутри процесса случайных молний");
        //To Do: add support to different priority target tablem for different lightning types
        var targets = _lookup.GetComponentsInRange<LightningTargetComponent>(Transform(user).Coordinates, range);
        var sortedTargets = targets
            .OrderByDescending(target => target.Priority)
            .ThenBy(_ => _random.Next())
            .ToList();
        var realCount = Math.Min(targets.Count, boltCount);

        Log.Debug("Целей: " + realCount);
        if (realCount <= 0)
            return;

        for (int i = 0; i < realCount; i++)
        {
            Log.Debug("Целимся в " + sortedTargets[i].Owner.ToString());
            ShootLightning(user, sortedTargets[i].Owner, lightningPrototype);

            if (arcDepth > 0)
            {
                ShootRandomLightnings(sortedTargets[i].Owner, range, 1, lightningPrototype, arcDepth - 1);
            }
        }
    }
}
