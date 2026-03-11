using System.Linq;
using System.Runtime.CompilerServices;
using Content.Server.Atmos.Reactions;
using Content.Shared.Atmos;
using Robust.Shared.Prototypes;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    private GasReactionPrototype[] _gasReactions = [];

    /// <summary>
    ///     List of gas reactions ordered by priority.
    /// </summary>
    public IEnumerable<GasReactionPrototype> GasReactions => _gasReactions;

    public override void InitializeGases()
    {
        base.InitializeGases();

        _gasReactions = _protoMan.EnumeratePrototypes<GasReactionPrototype>().ToArray();
        Array.Sort(_gasReactions, (a, b) => b.Priority.CompareTo(a.Priority));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override float GetHeatCapacityCalculation(float[] moles, bool space)
    {
        // Little hack to make space gas mixtures have heat capacity, therefore allowing them to cool down rooms.
        if (space && MathHelper.CloseTo(NumericsHelpers.HorizontalAdd(moles), 0f))
        {
            return Atmospherics.SpaceHeatCapacity;
        }

        Span<float> tmp = stackalloc float[moles.Length];
        NumericsHelpers.Multiply(moles, GasSpecificHeats, tmp);
        // Adjust heat capacity by speedup, because this is primarily what
        // determines how quickly gases heat up/cool.
        return MathF.Max(NumericsHelpers.HorizontalAdd(tmp), Atmospherics.MinimumHeatCapacity);
    }
}
