using System.Threading;
using Content.Server.AI.Components;
using Content.Server.AI.HTN.PrimitiveTasks;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.AI.HTN;

public sealed partial class HTNSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ISawmill _sawmill = default!;
    private readonly JobQueue _planQueue = new(0.005);

    // TODO: Move this onto JobQueue as a finishedjobs thing we can flush.

    // Hierarchical Task Network
    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("npc.htn");
        SubscribeLocalEvent<HTNComponent, ComponentStartup>(OnHTNStartup);
        SubscribeLocalEvent<HTNComponent, ComponentShutdown>(OnHTNShutdown);

        _prototypeManager.PrototypesReloaded += OnPrototypeLoad;
        OnLoad();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _prototypeManager.PrototypesReloaded -= OnPrototypeLoad;
    }

    private void OnLoad()
    {
        // Add dependencies for all operators.
        // We put code on operators as I couldn't think of a clean way to put it on systems.
        foreach (var compound in _prototypeManager.EnumeratePrototypes<HTNCompoundTask>())
        {
            UpdateCompound(compound);
        }

        foreach (var primitive in _prototypeManager.EnumeratePrototypes<HTNPrimitiveTask>())
        {
            UpdatePrimitive(primitive);
        }
    }

    private void OnPrototypeLoad(PrototypesReloadedEventArgs obj)
    {
        OnLoad();
    }

    private void UpdatePrimitive(HTNPrimitiveTask primitive)
    {
        foreach (var precon in primitive.Preconditions)
        {
            IoCManager.InjectDependencies(precon);
        }

        primitive.Operator.Initialize();
    }

    private void UpdateCompound(HTNCompoundTask compound)
    {
        foreach (var branch in compound.Branches)
        {
            branch.Tasks.Clear();
            branch.Tasks.EnsureCapacity(branch.TaskPrototypes.Count);

            // Didn't do this in a typeserializer because we can't recursively grab our own prototype during it, woohoo!
            foreach (var proto in branch.TaskPrototypes)
            {
                if (_prototypeManager.TryIndex<HTNCompoundTask>(proto, out var compTask))
                {
                    branch.Tasks.Add(compTask);
                }
                else if (_prototypeManager.TryIndex<HTNPrimitiveTask>(proto, out var primTask))
                {
                    branch.Tasks.Add(primTask);
                }
                else
                {
                    _sawmill.Error($"Unable to find HTNTask fopr {proto} on {compound.ID}");
                }
            }

            foreach (var precon in branch.Preconditions)
            {
                IoCManager.InjectDependencies(precon);
            }
        }
    }

    private void OnHTNStartup(EntityUid uid, HTNComponent component, ComponentStartup args)
    {
        EnsureComp<ActiveNPCComponent>(uid);
        component.BlackboardA.SetValue(NPCBlackboard.Owner, uid);
    }

    private void OnHTNShutdown(EntityUid uid, HTNComponent component, ComponentShutdown args)
    {
        RemComp<ActiveNPCComponent>(uid);

        component.PlanningToken?.Cancel();
    }

    /// <summary>
    /// Forces the NPC to replan.
    /// </summary>
    [PublicAPI]
    public void Replan(HTNComponent component)
    {
        component.PlanAccumulator = 0f;
    }

    public void UpdateNPC(ref int count, int maxUpdates, float frameTime)
    {
        _planQueue.Process();

        foreach (var (_, comp) in EntityQuery<ActiveNPCComponent, HTNComponent>())
        {
            if (count >= maxUpdates)
                break;

            if (comp.PlanningJob != null)
            {
                // If a new planning job has finished then handle it.
                if (comp.PlanningJob.Status != JobStatus.Finished)
                    continue;

                var newPlanBetter = true;

                // If old traversal is better than new traversal then ignore the new plan
                if (comp.Plan != null && comp.PlanningJob.Result != null)
                {
                    var oldMtr = comp.Plan.BranchTraversalRecord;
                    var mtr = comp.PlanningJob.Result.BranchTraversalRecord;

                    for (var i = 0; i < oldMtr.Count; i++)
                    {
                        // TODO: Fix.
                        if (i >= mtr.Count || oldMtr[i] < mtr[i])
                        {
                            newPlanBetter = false;
                            break;
                        }
                    }
                }

                if (newPlanBetter)
                {
                    comp.Plan = comp.PlanningJob.Result;

                    // Startup the first task and anything else we need to do.
                    if (comp.Plan != null)
                    {
                        StartupTask(comp.Plan.Tasks[comp.Plan.Index], comp.BlackboardA, comp.Plan.Effects[comp.Plan.Index]);
                    }
                }

                comp.PlanningJob = null;
                comp.PlanningToken = null;
            }

            Update(comp, frameTime);
            count++;
        }
    }

    private void Update(HTNComponent component, float frameTime)
    {
        // Get a new plan
        if (component.Plan == null)
        {
            RequestPlan(component);
            return;
        }

        // We'll still try re-planning occasionally even when we're updating in case new data comes in.
        if (component.PlanAccumulator <= 0f)
        {
            RequestPlan(component);
        }
        else
        {
            component.PlanAccumulator -= frameTime;
        }

        // Run the existing plan still
        var status = HTNOperatorStatus.Finished;

        // Continuously run operators until we can't anymore.
        while (status != HTNOperatorStatus.Continuing && component.Plan != null)
        {
            // Run the existing operator
            var currentOperator = component.Plan.CurrentOperator;
            var blackboard = component.BlackboardA;
            status = currentOperator.Update(blackboard, frameTime);

            switch (status)
            {
                case HTNOperatorStatus.Continuing:
                    break;
                case HTNOperatorStatus.Failed:
                    currentOperator.Shutdown(blackboard, status);
                    component.Plan = null;
                    break;
                // Operator completed so go to the next one.
                case HTNOperatorStatus.Finished:
                    currentOperator.Shutdown(blackboard, status);
                    component.Plan.Index++;

                    // Plan finished!
                    if (component.Plan.Tasks.Count <= component.Plan.Index)
                    {
                        component.Plan = null;
                        break;
                    }

                    StartupTask(component.Plan.Tasks[component.Plan.Index], component.BlackboardA, component.Plan.Effects[component.Plan.Index]);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// Starts a new primitive task. Will apply effects from planning if applicable.
    /// </summary>
    private void StartupTask(HTNPrimitiveTask primitive, NPCBlackboard blackboard, Dictionary<string, object>? effects)
    {
        // We may have planner only tasks where we want to reuse their data during update
        // e.g. if we pathfind to an enemy to know if we can attack it, we don't want to do another pathfind immediately
        if (effects != null && primitive.ApplyEffectsOnStartup)
        {
            foreach (var (key, value) in effects)
            {
                blackboard.SetValue(key, value);
            }
        }

        primitive.Operator.Startup(blackboard);
    }

    /// <summary>
    /// Request a new plan for this component, even if running an existing plan.
    /// </summary>
    /// <param name="component"></param>
    private void RequestPlan(HTNComponent component)
    {
        if (component.PlanningJob != null)
            return;

        component.PlanAccumulator += component.PlanCooldown;
        var cancelToken = new CancellationTokenSource();

        var job = new HTNPlanJob(
            0.02,
            _prototypeManager.Index<HTNCompoundTask>(component.RootTask),
            component.BlackboardA.ShallowClone(), cancelToken.Token);

        _planQueue.EnqueueJob(job);
        component.PlanningJob = job;
        component.PlanningToken = cancelToken;
    }
}

/// <summary>
/// The outcome of the current operator during update.
/// </summary>
public enum HTNOperatorStatus : byte
{
    Continuing,
    Failed,
    Finished,
}
