using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Materials;
using Content.Shared.Temperature.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// <para>Holds the composition of an entity made from reagents and its reagent temperature.</para>
/// <para>If the entity is used to represent a collection of reagents inside of a container such as a beaker, syringe, bloodstream, food, or similar the entity is tracked by a <see cref="SolutionManagerComponent"/> on the container and has a <see cref="ContainedSolutionComponent"/> tracking which container it's in.</para>
/// </summary>
/// <remarks>
/// <para>Once reagents and materials have been merged this component should be depricated in favor of using a combination of <see cref="PhysicalCompositionComponent"/> and <see cref="TemperatureComponent"/>. May require minor reworks to both.</para>
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class SolutionComponent : Component
{
    public const string DefaultSolutionId = "solution";

    /// <summary>
    /// The name of this solution. This value should *never* change once the solution is initialized.
    /// </summary>
    [DataField]
    [Access(typeof(SharedSolutionContainerSystem))]
    public string Id = DefaultSolutionId;

    /// <summary>
    /// <para>The reagents the entity is composed of and their temperature.</para>
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public Solution Solution = new();
}

/// <remarks>
/// We manually network the component state as it raises one less event and therefore is better performance wise.
/// </remarks>
[Serializable, NetSerializable]
public sealed class SolutionComponentState(Solution solution) : ComponentState
{
    public Solution Solution = solution;
}
