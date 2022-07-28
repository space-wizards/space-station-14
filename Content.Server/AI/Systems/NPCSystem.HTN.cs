using System.Threading;
using Content.Server.AI.Components;
using Content.Server.AI.HTN;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using Robust.Shared.Utility;

namespace Content.Server.AI.Systems;

public sealed partial class NPCSystem
{
    private readonly HashSet<EntityUid> _planning = new();
    private readonly JobQueue _planQueue = new(0.005);

    // TODO: Move this onto JobQueue as a finishedjobs thing we can flush.
    private readonly Dictionary<EntityUid, (HTNPlanJob Job, CancellationTokenSource CancelToken)> _jobs = new();

    // Hierarchical Task Network
    private void InitializeHTN()
    {
        SubscribeLocalEvent<HTNComponent, ComponentShutdown>(OnHTNShutdown);
    }

    private void OnHTNShutdown(EntityUid uid, HTNComponent component, ComponentShutdown args)
    {
        if (_jobs.TryGetValue(uid, out var job))
        {
            job.CancelToken.Cancel();
            _jobs.Remove(uid);
        }

        _planning.Remove(uid);
    }

    private void UpdateHTN(float frameTime)
    {
        _planQueue.Process();

        foreach (var (_, comp) in EntityQuery<ActiveNPCComponent, HTNComponent>())
        {
            if (_count >= _maxUpdates)
                break;

            if (_jobs.TryGetValue(comp.Owner, out var job))
            {
                if (job.Job.Status != JobStatus.Finished)
                    continue;

                comp.Plan = job.Job.Result;
                _jobs.Remove(comp.Owner);
            }

            Update(comp, frameTime);
            _count++;
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

        // Run the existing operator
    }

    private void RequestPlan(HTNComponent component)
    {
        if (!_planning.Add(component.Owner)) return;

        var cancelToken = new CancellationTokenSource();

        var job = new HTNPlanJob(0.02, _prototypeManager.Index<HTNCompoundTask>(component.RootTask),
            (NPCBlackboard) component.BlackboardA.ShallowClone(), cancelToken.Token);

        _planQueue.EnqueueJob(job);
        _jobs.Add(component.Owner, (job, cancelToken));
    }
}
