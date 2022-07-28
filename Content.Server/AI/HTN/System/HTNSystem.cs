using System.Threading;
using Content.Server.AI.Components;
using Content.Server.AI.HTN.PrimitiveTasks;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using Robust.Shared.Prototypes;

namespace Content.Server.AI.HTN;

public sealed partial class HTNSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly JobQueue _planQueue = new(0.005);

    // TODO: Move this onto JobQueue as a finishedjobs thing we can flush.

    // Hierarchical Task Network
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HTNComponent, ComponentStartup>(OnHTNStartup);
        SubscribeLocalEvent<HTNComponent, ComponentShutdown>(OnHTNShutdown);

        _prototypeManager.PrototypesReloaded += OnPrototypeLoad;
        OnLoad();
    }

    private void OnLoad()
    {
        // Add dependencies for all operators.
        // We put code on operators as I couldn't think of a clean way to put it on systems.
        foreach (var compound in _prototypeManager.EnumeratePrototypes<HTNCompoundTask>())
        {
            UpdateCompound(compound);
        }
    }

    private void OnPrototypeLoad(PrototypesReloadedEventArgs obj)
    {
        OnLoad();
    }

    private void UpdateCompound(HTNCompoundTask compound)
    {
        foreach (var branch in compound.Branches)
        {
            foreach (var task in branch.Tasks)
            {
                switch (task)
                {
                    case HTNCompoundTask compoundy:
                        UpdateCompound(compoundy);
                        break;
                    case HTNPrimitiveTask primitive:
                        primitive.Operator.Initialize();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _prototypeManager.PrototypesReloaded -= OnPrototypeLoad;
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

    public void UpdateNPC(ref int count, int maxUpdates, float frameTime)
    {
        _planQueue.Process();

        foreach (var (_, comp) in EntityQuery<ActiveNPCComponent, HTNComponent>())
        {
            if (count >= maxUpdates)
                break;

            if (comp.PlanningJob != null)
            {
                if (comp.PlanningJob.Status != JobStatus.Finished)
                    continue;

                comp.Plan = comp.PlanningJob.Result;
                comp.PlanningJob = null;
                comp.PlanningToken = null;

                // Startup the first task and anything else we need to do.
                if (comp.Plan != null)
                {
                    comp.Plan.Tasks[comp.Plan.Index].Operator.Startup(comp.BlackboardA);
                }
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

        // Run the existing plan still
        var currentOperator = component.Plan.CurrentOperator;
        var blackboard = component.BlackboardA;

        // Run the existing operator
        var status = currentOperator.Update(blackboard, frameTime);

        switch (status)
        {
            case HTNOperatorStatus.Continuing:
                break;
            case HTNOperatorStatus.Failed:
                component.Plan = null;
                break;
            // Operator completed so go to the next one.
            case HTNOperatorStatus.Finished:
                component.Plan.Index++;

                // Plan finished!
                if (component.Plan.Tasks.Count <= component.Plan.Index)
                {
                    component.Plan = null;
                    break;
                }

                component.Plan.Tasks[component.Plan.Index].Operator.Startup(component.BlackboardA);
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    private void RequestPlan(HTNComponent component)
    {
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

public enum HTNOperatorStatus : byte
{
    Continuing,
    Failed,
    Finished,
}
