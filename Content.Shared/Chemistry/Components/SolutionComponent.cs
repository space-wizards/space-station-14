using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Types;
using Content.Shared.FixedPoint;
using Content.Shared.Materials;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// <para>Holds the composition of an entity made from reagents and its reagent temperature.</para>
/// <para>If the entity is used to represent a collection of reagents inside of a container such as a beaker, syringe, bloodstream, food, or similar the entity is tracked by a <see cref="SolutionContainerManagerComponent"/> on the container and has a <see cref="ContainedSolutionComponent"/> tracking which container it's in.</para>
/// </summary>
/// <remarks>
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SolutionComponent : Component
{
    /// <summary>
    /// <para>The reagents the entity is composed of and their temperature.</para>
    /// </summary>
    [DataField, AutoNetworkedField, Obsolete]
    public Solution Solution = new(); //TODO: legacy remove this

    [DataField]
    public EntityUid SolutionOwner = EntityUid.Invalid;

    [DataField, AutoNetworkedField]
    public List<ReagentQuantity> Contents = new();

    /// <summary>
    ///     The name of this solution
    /// </summary>
    [DataField]
    public string Name;

    [DataField]
    public bool CanOverflow = true;

    public FixedPoint2 OverflowAmount => Volume - MaxVolume;

    /// <summary>
    ///     If reactions will be checked for when adding reagents to the container.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("canReact")]
    public bool CanReact { get; set; } = true;

    /// <summary>
    ///     Checks if a solution can fit into the container.
    /// </summary>
    public bool CanAddSolution(Solution solution)
    {
        return !CanOverflow && solution.Volume <= AvailableVolume;
    }

    /// <summary>
    ///     The calculated total volume of all reagents in the solution (ex. Total volume of liquid in beaker).
    /// </summary>
    [ViewVariables]
    public FixedPoint2 Volume { get; set; }

    /// <summary>
    ///     Maximum volume this solution supports.
    /// </summary>
    /// <remarks>
    ///     A value of zero means the maximum will automatically be set equal to the current volume during
    ///     initialization. Note that most solution methods ignore max volume altogether, but various solution
    ///     systems use this.
    /// </remarks>
    [DataField("maxVol")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxVolume { get; set; } = FixedPoint2.Zero;

    public float FillFraction => MaxVolume == 0 ? 1 : Volume.Float() / MaxVolume.Float();

    /// <summary>
    ///     Volume needed to fill this container.
    /// </summary>
    [ViewVariables]
    public FixedPoint2 AvailableVolume => MaxVolume - Volume;

    //TODO: just use temperature component dear god
    /// <summary>
    ///     The temperature of the reagents in the solution.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("temperature")]
    public float Temperature { get; set; } = 293.15f;

    /// <summary>
    ///     The total heat capacity of all reagents in the solution.
    /// </summary>
    [ViewVariables] public float HeatCapacity;
}
