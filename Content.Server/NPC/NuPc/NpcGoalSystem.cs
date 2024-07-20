using System.Runtime.InteropServices;
using Content.Server.Administration.Managers;
using Content.Shared.NPC.NuPC;
using Robust.Shared.Collections;
using Robust.Shared.Player;
using Robust.Shared.Threading;
using Robust.Shared.Utility;

namespace Content.Server.NPC.NuPc;

public sealed partial class NpcGoalSystem : SharedNpcGoalSystem
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IParallelManager _parallel = default!;

    private NpcGoalJob _goalJob = new();

    private readonly List<Entity<NpcKnowledgeComponent, NuPcComponent>> _npcs = new();

    private List<ICommonSession> _debugSubscribers = new();

    public override void Initialize()
    {
        base.Initialize();

        _goalJob.System = this;
        _goalJob.Npcs = _npcs;

        SubscribeNetworkEvent<RequestNpcGoalsEvent>(OnGoalsRequest);

        // If an NPC updates on Tick 0 then we update goals on Tick 1.
        UpdatesBefore.Add(typeof(NpcKnowledgeSystem));
    }

    private void OnGoalsRequest(RequestNpcGoalsEvent ev, EntitySessionEventArgs args)
    {
        if (!_admins.IsAdmin(args.SenderSession))
            return;

        if (ev.Enabled)
        {
            if (!_debugSubscribers.Contains(args.SenderSession))
                return;

            _debugSubscribers.Add(args.SenderSession);
        }
        else
        {
            _debugSubscribers.Remove(args.SenderSession);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NpcKnowledgeComponent, NuPcComponent>();
        _npcs.Clear();

        while (query.MoveNext(out var uid, out var knowledge, out var npc))
        {
            // If knowledge hasn't updated then can't update goals.
            if (!knowledge.CanUpdate ||
                knowledge.GoalGenerators.Count == 0)
            {
                continue;
            }

            _npcs.Add((uid, knowledge, npc));
        }

        // Yippee
        if (_npcs.Count == 0)
            return;

        _parallel.ProcessNow(_goalJob, _npcs.Count);

        if (_debugSubscribers.Count > 0)
        {
            var debugQuery = EntityQueryEnumerator<NpcKnowledgeComponent>();
            var ev = new NpcGoalsDebugEvent();

            while (debugQuery.MoveNext(out var uid, out var knowledge))
            {
                if (!knowledge.Enabled)
                    continue;

                var data = new NpcGoalsData
                {
                    Generators = knowledge.GoalGenerators,
                    Goals = knowledge.Goals,
                };

                ev.Data.Add(data);
            }

            RaiseNetworkEvent(ev, Filter.Empty().AddPlayers(_debugSubscribers));
        }
    }

    /// <summary>
    /// Uses data store on <see cref="NpcKnowledgeComponent"/> to update goals accordingly.
    /// </summary>
    private record struct NpcGoalJob() : IParallelRobustJob
    {
        public NpcGoalSystem System;

        public List<Entity<NpcKnowledgeComponent, NuPcComponent>> Npcs;

        public void Execute(int index)
        {
            var npc = Npcs[index];
            npc.Comp1.CanUpdate = false;

            // Block so we don't accidentally re-use the span.
            {
                var goalSpan = CollectionsMarshal.AsSpan(npc.Comp1.Goals);

                for (var i = 0; i < goalSpan.Length; i++)
                {
                    ref var goal = ref goalSpan[i];
                    goal.Updated = false;
                }
            }

            // New goals.
            var goals = new ValueList<INpcGoal>();

            // Run generators and get new goals.
            foreach (var generator in npc.Comp1.GoalGenerators)
            {
                // TODO: Check if generators supposed to be 1-1

                // Note that goal generation should only ever work off of NPC knowledge and not the game sim directly.
                // This is so we can abstract and share the logic for different behaviors.
                switch (generator)
                {
                    case NpcChaseGoalGenerator chase:
                        System.GetGoal(ref goals, npc.Comp1, chase);
                        break;
                    case NpcCombatGoalGenerator combat:
                        System.GetGoal(ref goals, npc.Comp1, combat);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var nuGoal in goals)
            {
                if (npc.Comp1.Goals.Contains(nuGoal))
                    continue;

                npc.Comp1.Goals.Add(nuGoal);
            }

            // Remove stale goals.
            {
                for (var i = 0; i < npc.Comp1.Goals.Count; i++)
                {
                    var goal = npc.Comp1.Goals[i];

                    if (!goal.Updated)
                    {
                        npc.Comp1.Goals.RemoveSwap(i--);
                    }
                }
            }
        }
    }
}
