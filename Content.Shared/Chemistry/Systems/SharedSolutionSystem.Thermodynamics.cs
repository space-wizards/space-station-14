using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Types;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    protected void ChangeTotalVolume(Entity<SolutionComponent> solution, FixedPoint2 delta)
    {
        SetTotalVolume(solution, solution.Comp.Volume + delta);
    }

    protected void SetTotalVolume(Entity<SolutionComponent> solution, FixedPoint2 newVolume)
    {
        newVolume = FixedPoint2.Max(0, newVolume);
        if (newVolume == solution.Comp.Volume)
            return;
        solution.Comp.Volume = newVolume;
        Dirty(solution);
    }
}
