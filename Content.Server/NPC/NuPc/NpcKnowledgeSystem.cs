using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Collections;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Random;
using Robust.Shared.Threading;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NpcKnowledgeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private NpcGoalJob _goalJob = new();
    private NpcKnowledgeJob _knowledgeJob = new();

    private readonly List<Entity<NpcKnowledgeComponent>> _npcs = new();

    /// <summary>
    /// Maximum update duration. Any stims older than this will be purged as they're no longer relevant for NPCs.
    /// </summary>
    public TimeSpan MaxUpdate;

    // TODO: Even though hostile mobs in range need to handle pathing or whatever so?

    public override void Initialize()
    {
        base.Initialize();
        _goalJob.Npcs = _npcs;
        _knowledgeJob.Npcs = _npcs;

        SubscribeLocalEvent<NpcKnowledgeComponent, MapInitEvent>(OnKnowledgeMapInit);
        SubscribeLocalEvent<NpcKnowledgeComponent, MobStateChangedEvent>(OnMobChanged);
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
            // TODO: Clear data.
            entity.Comp.Goals.Clear();
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

    public void CreateStim()
    {

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NpcKnowledgeComponent>();
        var curTime = _timing.CurTime;

        // Setup knowledge update.
        _npcs.Clear();
        _knowledgeJob.GameTime = curTime;
        // Go through stims and cache MapCoordinates.
        // TODO:

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Enabled || comp.NextUpdate > curTime)
                continue;

            _npcs.Add((uid, comp));
        }

        _parallel.ProcessNow(_knowledgeJob, _npcs.Count);
        _parallel.ProcessNow(_goalJob, _npcs.Count);
    }

    /// <summary>
    /// Uses NPC sensors to update their <see cref="NpcKnowledgeComponent"/>.
    /// </summary>
    private record struct NpcKnowledgeJob() : IParallelRobustJob
    {
        public TimeSpan GameTime;
        public List<Entity<NpcKnowledgeComponent>> Npcs;

        public void Execute(int index)
        {
            var npc = Npcs[index];

            // Expire old stims
            npc.Comp.VisualStims.Clear();

            foreach (var stim in npc.Comp.AudioStims)
            {
                if (stim.EndTime < GameTime)
                {
                    continue;
                }
            }

            foreach (var stim in npc.Comp.GenericStims)
            {
                if (stim.EndTime < GameTime)
                {
                    continue;
                }
            }

            // Mobs get special-cased because if someone goes out of range we might still care
            // to try and chase the target so need to create a separate entry for it.
            var newMobs = new ValueList<EntityUid>();

            // Get new stims.
            foreach (var sensor in npc.Comp.Sensors)
            {
                var shape = sensor.Shape;

                // Get mobs in range as these don't have discrete stim entries.
                foreach (var mob in new List<Entity<MobStateComponent>>())
                {
                    // Raycast required
                    if (sensor.CollisionMask != 0)
                    {
                        continue;
                    }

                    // Check if we already have it.
                    if (npc.Comp.HostileMobs.Contains())
                        continue;

                    newMobs.Add(mob.Owner);
                }

                // Get new audio / visual stims
                // TODO: Check if the stim time is in the last
            }

            // Expire old hostile-mob stims.
            for (var i = npc.Comp.HostileMobs.Count - 1; i >= 0; i--)
            {
                var mob = npc.Comp.HostileMobs[i];

                if (newMobs.Contains(mob.Owner))
                    continue;

                // Not in range, remove it
                npc.Comp.HostileMobs.RemoveAt(i);
                // TODO: Create target lost stim.
            }

            npc.Comp.NextUpdate += npc.Comp.UpdateCooldown;
        }
    }

    /// <summary>
    /// Uses data store on <see cref="NpcKnowledgeComponent"/> to update goals accordingly.
    /// </summary>
    private record struct NpcGoalJob() : IParallelRobustJob
    {
        public List<Entity<NpcKnowledgeComponent>> Npcs;

        public void Execute(int index)
        {
            var npc = Npcs[index];

            foreach (var generator in npc.Comp.GoalGenerators)
            {

            }
        }
    }
}

/// <summary>
/// Handles what an NPC can "see" / "hear" in the game.
/// Every update these get iterated and are integral to keeping <see cref="NpcKnowledgeComponent"/> up to date.
/// </summary>
public sealed class NpcSensor
{
    /// <summary>
    /// Shape of this sensor.
    /// </summary>
    [DataField(required: true)]
    public IPhysShape Shape = new PhysShapeCircle(10f);

    /// <summary>
    /// Should we check if the target stim is InRangeUnobstructed
    /// </summary>
    [DataField]
    public bool Unobstructed = false;

    /// <summary>
    /// What stims this sensor can react to.
    /// </summary>
    [DataField(required: true)]
    public List<Type> ValidStims = new();
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
            Unobstructed = true,
            ValidStims = new()
            {
                typeof(HostileMobStim),
            },
        },
        // Audio
        new NpcSensor()
        {

        }
    };

    /*
     * Special-case stims that are frequently accessed.
     */

    [DataField]
    public List<HostileMobStim> HostileMobs = new();

    // Compound data here
    // Goals removed after 1 update if not refreshed.
    // Gunshot audio stim could create react goal with stim linked.
    // Maybe make stims classes and have visual / audio be abstract classes instead.
    // GoalGenerators work off of stims / knowledge.

    // Additional data here
}

public abstract class GoalGenerator
{

}

/// <summary>
/// Represents a visible hostile mob.
/// </summary>
public record struct HostileMobStim
{
    public EntityUid Owner;
}
