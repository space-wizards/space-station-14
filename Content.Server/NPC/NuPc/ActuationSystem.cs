using Content.Server.NPC.NuPc;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Systems;

public sealed class ActuationSystem : EntitySystem
{
    [Dependency] private readonly IParallelManager _parallel = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private NpcBehaviorSelectionJob _job = new();
    private List<Entity<NuPcComponent>> _npcs = new();

    private record struct NpcBehaviorSelectionJob : IParallelRobustJob
    {
        public ActuationSystem System;
        public List<Entity<NuPcComponent>> Npcs;

        public void Execute(int index)
        {
            var npc = Npcs[index];
            System.UpdateNpc(npc);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        _job.System = this;
        _job.Npcs = _npcs;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Update decisions for thinking NPCs.
        _npcs.Clear();
        var query = EntityQueryEnumerator<NuPcComponent>();

        while (query.MoveNext(out var uid, out var npc))
        {
            // Update current-behaviors. These might add / remove components so done in a thread-safe way.
            // It's also done for "dumb" clients without any regards for knowledge / re-running behaviors

            // TODO: Melee target selections needs to use these.
            var current = npc.CurrentBehavior;
            UpdateBehavior(current);

            _npcs.Add((uid, npc));
        }

        _parallel.ProcessNow(_job, _npcs.Count);
    }

    private void UpdateNpc(NuPcComponent component)
    {
        var current = component.CurrentBehavior;

        // Evaluate high-priority options
        var behaviors = new ValueList<ProtoId<NpcBehaviorPrototype>>();

        // TODO: OR time elapsed.
        if (current?.Status != NpcBehaviorState.Running)
        {
            // Regular behavior selection
            foreach (var behavior in component.Behaviors)
            {
                if (!_protoManager.TryIndex(behavior, out var groupProto))
                    continue;

                GetBehaviorOptions(groupProto, ref behaviors);
            }
        }

        // Select new behavior.
        if (behaviors.Count > 0)
        {
            var newBehavior = GetBestBehavior(current, ref behaviors);

            // If it's the same behavior don't re-select.
            if (newBehavior.ID != component.CurrentBehavior?.Behavior)
            {
                // Shutdown old & start new.
            }
        }

    }

    private void UpdateBehavior(NpcRunningBehavior? behavior)
    {
        if (behavior == null)
            return;

        // TODO: Update current action
    }

    private NpcBehaviorPrototype GetBestBehavior(NpcRunningBehavior? current, ref ValueList<ProtoId<NpcBehaviorPrototype>> prototypes)
    {
        DebugTools.Assert(prototypes.Count > 0, "Expected more than 1 behavior to select from!");

        if (prototypes.Count == 1)
        {
            return _protoManager.Index(prototypes[0]);
        }

        var scores = new ValueList<(float Score, NpcBehaviorPrototype Prototype)>(prototypes.Count);
        var highestScore = 0f;

        // TODO: Get scores, find the highest tied scores and pick randomly
        foreach (var proto in prototypes)
        {
            var behavior = _protoManager.Index(proto);
            var score = GetScore(behavior);

            highestScore = MathF.Max(highestScore, score);
            scores.Add((score, behavior));
        }

        // Remove any scores that are lower.
        for (var i = scores.Count - 1; i >= 0; i--)
        {
            var (score, proto) = scores[i];

            if (score >= highestScore)
                continue;

            scores.RemoveSwap(i);
        }

        // Check if any of the highest-scoring behaviors match the current behavior.
        // We do this to avoid flip-flopping constantly.
        if (current != null)
        {
            foreach (var (_, proto) in scores)
            {
                if (current.Behavior == proto.ID)
                {
                    return proto;
                }
            }
        }

        // Otherwise pick one randomly.
        var bestBehavior = _random.Pick(scores);
        return bestBehavior.Prototype;
    }

    /// <summary>
    /// Gets the score for the prototype.
    /// </summary>
    /// <remarks>
    /// We pass in the existing score so we can early-out if this score is going to be lower.
    /// </remarks>
    private float GetScore(NpcBehaviorPrototype behavior)
    {

    }

    private void GetBehaviorOptions(NpcBehaviorGroupPrototype group, ref ValueList<ProtoId<NpcBehaviorPrototype>> behaviors)
    {
        var isValid = true;

        foreach (var con in group.Preconditions)
        {
            if (!true)
            {
                isValid = false;
                break;
            }
        }

        if (!isValid)
            return;

        foreach (var sub in group.SubGroups)
        {
            if (!_protoManager.TryIndex(sub, out var subGroupProto))
                continue;

            GetBehaviorOptions(subGroupProto, ref behaviors);
        }

        foreach (var groupBehavior in group.Behaviors)
        {
            // If is valid put in behavioroptions
            if (!_protoManager.TryIndex(groupBehavior, out var behavior))
                continue;

            // TODO: Check precons
        }
    }
}

public sealed class NpcRunningBehavior
{
    [DataField]
    public NpcBehaviorState Status = NpcBehaviorState.Running;

    /// <summary>
    /// Behavior that spawned this.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<NpcBehaviorPrototype> Behavior;
}

/// <summary>
/// A group of <see cref="NpcBehaviorPrototype"/>.
/// </summary>
[Prototype]
public sealed partial class NpcBehaviorGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// Conditions that have to be true for this group to run.
    /// </summary>
    [DataField]
    public List<INpcPrecondition> Preconditions = new();

    /// <summary>
    /// Sub-groups we can iterate and include behaviors for.
    /// </summary>
    [DataField]
    public List<ProtoId<NpcBehaviorGroupPrototype>> SubGroups = new();

    /// <summary>
    /// Behaviors to include in the group.
    /// </summary>
    [DataField]
    public List<ProtoId<NpcBehaviorPrototype>> Behaviors = new();
}

[Prototype]
public sealed partial class NpcBehaviorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// Conditions that have to be true for this group to run.
    /// </summary>
    [DataField]
    public List<INpcPrecondition> Preconditions = new();

    // Cooldown
    [DataField]
    public TimeSpan Cooldown = TimeSpan.Zero;

    // Deita
    // TODO: Need some way to pass the things from the goal to scoring.
    // Maybe a struct for it? idk

    // Scoring
    [DataField(required: true)]
    public List<INpcScore> Score = new();

    /// <summary>
    /// The list of actions to run for this behavior.
    /// </summary>
    [DataField(required: true)]
    public List<INpcAction> Sequence = new();
}

[DataRecord]
public record struct NpcMoveTo : INpcAction
{

}

[DataRecord]
public record struct NpcMelee : INpcAction
{

}

/// <summary>
/// Precondition for running a behavior / group.
/// </summary>
public interface INpcPrecondition
{

}

/// <summary>
/// Action inside of a sequence tree, e.g. moveto, attack, eat, etc.
/// These wrap another component which handles the underlying actions independently.
/// </summary>
public interface INpcAction
{

}

/// <summary>
/// Scorer for a behavior.
/// </summary>
public interface INpcScore
{

}

public enum NpcBehaviorState : byte
{
    Running,
    Complete,
    Failed,
}
