using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Materials;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// <para>Holds the composition of an entity made from reagents and its reagent temperature.</para>
/// <para>If the entity is used to represent a collection of reagents inside of a container such as a beaker, syringe, bloodstream, food, or similar the entity is tracked by a <see cref="SolutionContainerManagerComponent"/> on the container and has a <see cref="ContainedSolutionComponent"/> tracking which container it's in.</para>
/// </summary>
/// <remarks>
/// <para>Once reagents and materials have been merged this component should be depricated in favor of using a combination of <see cref="PhysicalCompositionComponent"/> and <see cref="Content.Server.Temperature.Components.TemperatureComponent"/>. May require minor reworks to both.</para>
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SolutionComponent : Component
{
    /// <summary>
    /// <para>The reagents the entity is composed of and their temperature.</para>
    /// </summary>
    [DataField, AutoNetworkedField]
    public Solution Solution = new();
}
