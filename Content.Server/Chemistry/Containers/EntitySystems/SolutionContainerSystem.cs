using Content.Shared.Chemistry.Containers.Components;
using Content.Shared.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Solutions;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.Containers.EntitySystems;

public sealed partial class SolutionContainerSystem : SharedSolutionContainerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionContainerManagerComponent, ComponentInit>(InitSolution);
    }


    /// <summary>
    /// Will ensure a solution is added to given entity even if it's missing solutionContainerManager
    /// </summary>
    /// <param name="uid">EntityUid to which to add solution</param>
    /// <param name="name">name for the solution</param>
    /// <param name="solutionsMgr">solution components used in resolves</param>
    /// <param name="existed">true if the solution already existed</param>
    /// <returns>solution</returns>
    public Solution EnsureSolution(EntityUid uid, string name, out bool existed,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (!Resolve(uid, ref solutionsMgr, false))
        {
            solutionsMgr = EntityManager.EnsureComponent<SolutionContainerManagerComponent>(uid);
        }

        if (!solutionsMgr.Solutions.TryGetValue(name, out var existing))
        {
            var newSolution = new Solution() { Name = name };
            solutionsMgr.Solutions.Add(name, newSolution);
            existed = false;
            return newSolution;
        }

        existed = true;
        return existing;
    }

    /// <summary>
    /// Will ensure a solution is added to given entity even if it's missing solutionContainerManager
    /// </summary>
    /// <param name="uid">EntityUid to which to add solution</param>
    /// <param name="name">name for the solution</param>
    /// <param name="solutionsMgr">solution components used in resolves</param>
    /// <returns>solution</returns>
    public Solution EnsureSolution(EntityUid uid, string name, SolutionContainerManagerComponent? solutionsMgr = null)
        => EnsureSolution(uid, name, out _, solutionsMgr);

    /// <summary>
    /// Will ensure a solution is added to given entity even if it's missing solutionContainerManager
    /// </summary>
    /// <param name="uid">EntityUid to which to add solution</param>
    /// <param name="name">name for the solution</param>
    /// <param name="minVol">Ensures that the solution's maximum volume is larger than this value.</param>
    /// <param name="solutionsMgr">solution components used in resolves</param>
    /// <returns>solution</returns>
    public Solution EnsureSolution(EntityUid uid, string name, FixedPoint2 minVol, out bool existed,
        SolutionContainerManagerComponent? solutionsMgr = null)
    {
        if (!Resolve(uid, ref solutionsMgr, false))
        {
            solutionsMgr = EntityManager.EnsureComponent<SolutionContainerManagerComponent>(uid);
        }

        if (!solutionsMgr.Solutions.TryGetValue(name, out var existing))
        {
            var newSolution = new Solution() { Name = name };
            solutionsMgr.Solutions.Add(name, newSolution);
            existed = false;
            newSolution.MaxVolume = minVol;
            return newSolution;
        }

        existed = true;
        existing.MaxVolume = FixedPoint2.Max(existing.MaxVolume, minVol);
        return existing;
    }

    #region Event Handlers

    private void InitSolution(EntityUid uid, SolutionContainerManagerComponent component, ComponentInit args)
    {
        foreach (var (name, solutionHolder) in component.Solutions)
        {
            solutionHolder.Name = name;
            solutionHolder.ValidateSolution();
            SolutionSystem.UpdateAppearance(uid, solutionHolder);
        }
    }

    #endregion Event Handlers
}
