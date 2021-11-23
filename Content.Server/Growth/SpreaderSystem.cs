using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Shared.Atmos;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Random;

namespace Content.Server.Growth;

// Future work includes making the growths per interval thing not global, but instead per "group"
public class SpreaderSystem : EntitySystem
{
    /// <summary>
    /// Maximum number of edges that can grow out every interval.
    /// </summary>
    private const int GrowthsPerInterval = 1;

    private float _accumulatedFrameTime = 0.0f;
    private float _slowAccumulatedFrameTime = 0.0f;

    private readonly HashSet<EntityUid> _edgeGrowths = new ();
    private readonly Dictionary<EntityUid, HashSet<EntityUid>> _statusCapableInContact = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<SpreaderComponent, ComponentAdd>(SpreaderAddHandler);
        SubscribeLocalEvent<SpreaderComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<SpreaderComponent, EndCollideEvent>(OnEntityExit);
    }

    private void OnEntityExit(EntityUid uid, SpreaderComponent component, EndCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.OwnerUid;
        if (!EntityManager.HasComponent<StatusEffectsComponent>(otherUid))
            return;
        Logger.Debug($"exit");
        if (!_statusCapableInContact.ContainsKey(otherUid))
            _statusCapableInContact.Add(otherUid, new HashSet<EntityUid>());
        _statusCapableInContact[otherUid].Remove(uid);
    }

    private void OnEntityEnter(EntityUid uid, SpreaderComponent component, StartCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.OwnerUid;
        if (!EntityManager.HasComponent<StatusEffectsComponent>(otherUid))
            return;
        Logger.Debug($"enter");
        if (!_statusCapableInContact.ContainsKey(otherUid))
            _statusCapableInContact.Add(otherUid, new HashSet<EntityUid>());
        _statusCapableInContact[otherUid].Add(uid);
    }

    private void SpreaderAddHandler(EntityUid uid, SpreaderComponent component, ComponentAdd args)
    {
        _edgeGrowths.Add(uid); // ez
    }


    public void UpdateNearbySpreaders(EntityUid blocker, AirtightComponent comp)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(blocker, out var transform))
            return; // how did we get here?

        if (!_mapManager.TryGetGrid(transform.GridID, out var grid)) return;

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if (!comp.AirBlockedDirection.IsFlagSet(direction)) continue;

            foreach (var ent in grid.GetInDir(transform.Coordinates, direction.ToDirection()))
            {
                if (EntityManager.TryGetComponent<SpreaderComponent>(ent, out _))
                    _edgeGrowths.Add(ent);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);



        _accumulatedFrameTime += frameTime;
        _slowAccumulatedFrameTime += frameTime;

        if (_slowAccumulatedFrameTime >= 0.25)
        {
            UpdateSlows();
            _slowAccumulatedFrameTime -= 0.25f;
        }

        if (!(_accumulatedFrameTime >= 1.0f))
            return;

        _accumulatedFrameTime -= 1.0f;

        // Kudzu growing on to you.
        foreach (var ent in _statusCapableInContact.Keys)
        {
            if (_statusCapableInContact[ent].Count != 0)
                _sharedStunSystem.TrySlowdown(ent, TimeSpan.FromSeconds(2), 0.95f, 0.95f);
            else
                _statusCapableInContact.Remove(ent);
        }

        var growthList = _edgeGrowths.ToList();
        _robustRandom.Shuffle(growthList);

        var successes = 0;
        foreach (var entity in growthList)
        {
            if (!TryGrow(entity)) continue;

            successes += 1;
            if (successes >= GrowthsPerInterval)
                break;
        }
    }

    private void UpdateSlows()
    {
        foreach (var ent in _statusCapableInContact.Keys)
        {
            if (_statusCapableInContact[ent].Count != 0)
                _sharedStunSystem.TrySlowdown(ent, TimeSpan.FromSeconds(2), 0.85f, 0.85f);
            else
                _statusCapableInContact.Remove(ent);
        }
    }

    public bool TryGrow(EntityUid ent, TransformComponent? transform = null, SpreaderComponent? spreader = null)
    {
        if (!Resolve(ent, ref transform, ref spreader, false))
            return false;

        if (!_mapManager.TryGetGrid(transform.GridID, out var grid)) return false;

        var didGrow = false;

        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag) (1 << i);
            var ents = grid.GetInDir(transform.Coordinates, direction.AsDir()).ToArray();

            if (ents.Any(x => IsTileBlockedFrom(x, direction))) continue;

            // Ok, spawn a plant
            didGrow = true;
            EntityManager.SpawnEntity(spreader.GrowthResult, transform.Coordinates.Offset(direction.AsDir().ToVec()));
        }

        return didGrow;
    }

    private bool IsTileBlockedFrom(EntityUid ent, DirectionFlag dir)
    {
        if (EntityManager.TryGetComponent<SpreaderComponent>(ent, out _))
            return true;

        if (!EntityManager.TryGetComponent<AirtightComponent>(ent, out var airtight))
            return false;

        var oppositeDir = dir.AsDir().GetOpposite().ToAtmosDirection();

        return airtight.AirBlocked && airtight.AirBlockedDirection.IsFlagSet(oppositeDir);
    }

    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private SharedStunSystem _sharedStunSystem = default!;
}
