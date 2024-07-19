using System.Runtime.InteropServices;
using Content.Server.Administration.Managers;
using Content.Server.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.NuPC;
using Robust.Shared.Collections;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NPC.NuPc;

public sealed class NpcKnowledgeSystem : SharedNpcKnowledgeSystem
{
    [Dependency] private readonly ComponentTreeSystem<OccluderTreeComponent, OccluderComponent> _occluderTrees = default!;
    [Dependency] private readonly EntityLookupSystem _lookups = default!;
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ExamineSystem _examines = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;

    private NpcKnowledgeJob _knowledgeJob = new();

    private readonly List<Entity<NpcKnowledgeComponent>> _npcs = new();

    private HashSet<ICommonSession> _debugSubscribers = new();

    public static readonly TimeSpan LastKnownPositionDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum update duration. Any stims older than this will be purged as they're no longer relevant for NPCs.
    /// </summary>
    public TimeSpan MaxUpdate;

    public override void Initialize()
    {
        base.Initialize();

        _knowledgeJob.EntityManager = EntityManager;
        _knowledgeJob.Npcs = _npcs;
        _knowledgeJob.Examines = _examines;
        _knowledgeJob.Lookup = _lookups;
        _knowledgeJob.Transforms = _xforms;

        SubscribeLocalEvent<NpcKnowledgeComponent, MapInitEvent>(OnKnowledgeMapInit);
        SubscribeLocalEvent<NpcKnowledgeComponent, MobStateChangedEvent>(OnMobChanged);

        SubscribeNetworkEvent<RequestNpcKnowledgeEvent>(OnDebugRequest);
    }

    private void OnDebugRequest(RequestNpcKnowledgeEvent ev, EntitySessionEventArgs args)
    {
        if (!_admins.IsAdmin(args.SenderSession))
        {
            Log.Warning("Non-admin tried to request Npc knowledge data");
            return;
        }

        if (ev.Enabled)
        {
            _debugSubscribers.Add(args.SenderSession);
        }
        else
        {
            _debugSubscribers.Remove(args.SenderSession);
        }
    }

    private void OnMobChanged(Entity<NpcKnowledgeComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead && args.OldMobState != MobState.Dead)
            return;

        if (args.OldMobState == MobState.Dead)
        {
            SetEnabled(ent, false);
        }
        else
        {
            SetEnabled(ent, true);
        }
    }

    public void SetEnabled(Entity<NpcKnowledgeComponent> entity, bool value)
    {
        if (entity.Comp.Enabled == value)
            return;

        entity.Comp.Enabled = value;

        if (value)
        {
            entity.Comp.NextUpdate = _timing.CurTime;
        }
        else
        {
            // Clear data here.
            entity.Comp.HostileMobs.Clear();
        }
    }

    private void OnKnowledgeMapInit(Entity<NpcKnowledgeComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        // Give it a random update time so if we load many NPCs at once it doesn't put all of their updates on the same tick.
        ent.Comp.NextUpdate = _random.Next(ent.Comp.UpdateCooldown);
        MaxUpdate = TimeSpan.FromSeconds(Math.Max(ent.Comp.UpdateCooldown.TotalSeconds, MaxUpdate.TotalSeconds));
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NpcKnowledgeComponent>();
        var curTime = _timing.CurTime;


        // Setup knowledge update.
        _npcs.Clear();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled || comp.NextUpdate > curTime)
                continue;

            _npcs.Add((uid, comp));
        }

        // Early return nothing to do.
        if (_npcs.Count == 0)
            return;

        // TODO: This is a footgun waiting to happen OML
        _occluderTrees.UpdateTreePositions();

        _knowledgeJob.GameTime = curTime;
        _parallel.ProcessNow(_knowledgeJob, _npcs.Count);

        // Dump all debug data on clients.
        if (_debugSubscribers.Count > 0)
        {
            var debugQuery = EntityQueryEnumerator<NpcKnowledgeComponent>();
            var dataEv = new NpcKnowledgeDebugEvent();

            while (debugQuery.MoveNext(out var nUid, out var npc))
            {
                // I just like accessing struct elements by ref OKAY
                var mobSpan = CollectionsMarshal.AsSpan(npc.HostileMobs);

                for (var i = 0; i < mobSpan.Length; i++)
                {
                    ref var mobStim = ref mobSpan[i];
                    mobStim.DebugOwner = GetNetEntity(mobStim.Owner);
                }

                var lastPosSpan = CollectionsMarshal.AsSpan(npc.LastHostileMobPositions);

                for (var i = 0; i < lastPosSpan.Length; i++)
                {
                    ref var posStim = ref lastPosSpan[i];
                    posStim.DebugOwner = GetNetEntity(posStim.Owner);
                    posStim.DebugCoordinates = GetNetCoordinates(posStim.Coordinates);
                }

                dataEv.Data.Add(new NpcKnowledgeData()
                {
                    Entity = GetNetEntity(nUid),
                    Sensors = npc.Sensors,
                    HostileMobs = npc.HostileMobs,
                    LastHostileMobPositions = npc.LastHostileMobPositions,
                });
            }

            RaiseNetworkEvent(dataEv, Filter.Empty().AddPlayers(_debugSubscribers), false);
        }
    }

    /// <summary>
    /// Uses NPC sensors to update their <see cref="NpcKnowledgeComponent"/>.
    /// </summary>
    private record struct NpcKnowledgeJob() : IParallelRobustJob
    {
        public EntityManager EntityManager;
        public EntityLookupSystem Lookup;
        public ExamineSystem Examines;
        public SharedTransformSystem Transforms;

        public TimeSpan GameTime;
        public List<Entity<NpcKnowledgeComponent>> Npcs;

        public void Execute(int index)
        {
            var npc = Npcs[index];

            // Mobs get special-cased because if someone goes out of range we might still care
            // to try and chase the target so need to create a separate entry for it.
            var inRangeMobs = new ValueList<EntityUid>();
            var mobs = new HashSet<Entity<MobStateComponent>>();
            var xform = EntityManager.GetComponent<TransformComponent>(npc.Owner);
            var (worldPos, worldRot) = Transforms.GetWorldPositionRotation(xform);
            var transform = new Transform(worldPos, worldRot);

            // Get new stims.
            foreach (var sensor in npc.Comp.Sensors)
            {
                foreach (var flag in sensor.ValidStims)
                {
                    switch (flag)
                    {
                        case NpcSensorFlag.HostileMobs:
                        {
                            mobs.Clear();

                            Lookup.GetEntitiesIntersecting(xform.MapID,
                                sensor.Shape,
                                transform,
                                mobs);

                            // Get mobs in range as these don't have discrete stim entries.
                            foreach (var mob in mobs)
                            {
                                // If we already have it from another sensor then skip below checks.
                                if (npc.Owner == mob.Owner || inRangeMobs.Contains(mob.Owner))
                                    continue;

                                // Raycast required
                                if (sensor.Unoccluded && !Examines.InRangeUnOccluded(npc.Owner, mob.Owner, range: float.MaxValue))
                                {
                                    continue;
                                }

                                // Check if we already have it.
                                var stim = new HostileMobStim()
                                {
                                    Owner = mob.Owner
                                };

                                // Mark it as inrange even if we have a stim so we don't expire it later accidentally.
                                inRangeMobs.Add(mob.Owner);

                                if (npc.Comp.HostileMobs.Contains(stim))
                                    continue;

                                npc.Comp.HostileMobs.Add(stim);

                                // If we have a last known position then remove it.
                                for (var i = npc.Comp.LastHostileMobPositions.Count - 1; i >= 0; i--)
                                {
                                    var lastPos = npc.Comp.LastHostileMobPositions[i];

                                    if (lastPos.Owner == mob.Owner)
                                    {
                                        npc.Comp.LastHostileMobPositions.RemoveSwap(i);
                                        break;
                                    }
                                }
                            }

                            break;
                        }
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            // Expire old hostile-mob stims.
            for (var i = npc.Comp.HostileMobs.Count - 1; i >= 0; i--)
            {
                var mob = npc.Comp.HostileMobs[i];

                if (inRangeMobs.Contains(mob.Owner))
                    continue;

                // Not in range, remove it
                npc.Comp.HostileMobs.RemoveSwap(i--);
                var stim = new LastKnownHostilePositionStim()
                {
                    Owner = mob.Owner,
                    Coordinates = EntityManager.GetComponent<TransformComponent>(mob.Owner).Coordinates,
                    EndTime = GameTime + LastKnownPositionDuration,
                };

                npc.Comp.LastHostileMobPositions.Add(stim);
            }

            // Expire all other old stims
            for (var i = 0; i < npc.Comp.LastHostileMobPositions.Count; i++)
            {
                var lastPos = npc.Comp.LastHostileMobPositions[i];

                if (lastPos.EndTime > GameTime)
                    continue;

                npc.Comp.LastHostileMobPositions.RemoveSwap(i--);
            }

            npc.Comp.CanUpdate = true;
            npc.Comp.NextUpdate += npc.Comp.UpdateCooldown;
        }
    }
}

// TODO: Access
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class NpcKnowledgeComponent : Component
{
    /*
     * Setup data.
     */

    [DataField]
    public bool Enabled = true;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(0.3);

    [DataField, AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public List<NpcSensor> Sensors = new()
    {
        // Visual
        new NpcSensor()
        {
            Unoccluded = true,
            ValidStims = new()
            {
                NpcSensorFlag.HostileMobs,
            },
        },
        // Audio
        new NpcSensor(),
    };

    [DataField]
    public List<INpcGoalGenerator> GoalGenerators = new()
    {
        new NpcCombatGoalGenerator(),
        new NpcChaseGoalGenerator(),
    };

    /// <summary>
    /// Can the NPC update its decision based on new knowledge data.
    /// Flagged whenever the NPC knowledge update has run.
    /// </summary>
    [DataField]
    public bool CanUpdate;

    /*
     * Special-case stims that are frequently accessed.
     */

    [DataField]
    public List<HostileMobStim> HostileMobs = new();

    [DataField]
    public List<LastKnownHostilePositionStim> LastHostileMobPositions = new();

    // Compound data here
    [DataField]
    public List<INpcGoal> Goals = new();
}

[RegisterComponent]
public sealed partial class NuPcComponent : Component
{

}
