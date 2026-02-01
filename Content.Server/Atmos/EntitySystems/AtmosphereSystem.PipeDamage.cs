using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    /*
     Partial class for performing pipe pressure damage to pipes when they exceed their maximum pressure.

     The horrible truth: I genuinely despise resolving a million dependencies inside PipeNet and everything
     I need is already public on the PipeNet.
     */

    /// <summary>
    /// Dictionary that is modified to the amount of damage that's needed and then passed into methods
    /// instead of creating and garbaging a dict for every single pipe that needs damage.
    /// </summary>
    private readonly DamageSpecifier _pipeBurstingDamageSpecifier = new();

    /// <summary>
    /// Initializes the pipe damage specifier.
    /// </summary>
    private void InitializePipeDamage()
    {
        _pipeBurstingDamageSpecifier.DamageDict.Add("Structural", 0);
    }

    /// <summary>
    /// Performs damage on all pipe nodes in the given pipenet that exceed their maximum pressure.
    /// </summary>
    /// <param name="pipeNet">The pipenet to check for overpressure.</param>
    private void PerformPipeDamageOnAllNodes(IPipeNet? pipeNet)
    {
        if (pipeNet == null)
            return;

        // Check each pipe node for overpressure and apply damage if needed
        foreach (var node in pipeNet.Nodes)
        {
            // Node isn't a pipe or doesn't take pressure damage
            if (node is not PipeNode { MaxPressure: > 0 } pipe)
                continue;

            // Check to see if the node has an air-blocking
            // entity over it. We don't want to damage pipes under
            // stuff like walls otherwise it'll be super unintuitive to fix and not fun.
            var xform = Transform(node.Owner);
            if (xform.GridUid == null || !TryComp<MapGridComponent>(xform.GridUid, out var mapComp))
                continue;

            var xformGridUid = xform.GridUid.Value;
            var coords = _mapSystem.TileIndicesFor(xformGridUid, mapComp, xform.Coordinates);
            if (IsTileAirBlockedCached(xformGridUid, coords))
                continue;

            // Prefer damaging pipes that are already damaged. This means that only one pipe
            // fails instead of the whole pipenet bursting at the same time.
            const float baseChance = 0.5f;
            var p = baseChance;
            if (TryComp<DamageableComponent>(pipe.Owner, out var damage))
            {
                p += (float)damage.TotalDamage * (1 - baseChance);
            }

            var finalChance = Math.Clamp(1-p, 0f, 1f);
            if (_random.Prob(finalChance))
                continue;

            // close but no cigar
            var dam = PressureDamage(pipe);
            if (dam <= 0)
                continue;

            _pipeBurstingDamageSpecifier.DamageDict["Structural"] = dam;
            _damage.TryChangeDamage(pipe.Owner, _pipeBurstingDamageSpecifier);
            PryTile((xformGridUid, mapComp), coords);
        }
    }

    /// <summary>
    /// Calculate pressure damage for pipe. There is no damage if the pressure is below MaxPressure,
    /// and damage scales exponentially beyond that.
    /// </summary>
    private static int PressureDamage(PipeNode pipe)
    {
        const float tau = 10; // number of atmos ticks to break pipe at nominal overpressure
        var diff = pipe.Air.Pressure - pipe.MaxPressure;
        const float alpha = 100 / tau;
        return diff > 0 ? (int)(alpha * float.Exp(diff / pipe.MaxPressure)) : 0;
    }
}
