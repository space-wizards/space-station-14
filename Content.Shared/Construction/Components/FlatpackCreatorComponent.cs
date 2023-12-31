using Robust.Shared.GameStates;

namespace Content.Shared.Construction.Components;

/// <summary>
/// This is used for a machine that creates flatpacks at the cost of materials
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedFlatpackSystem))]
public sealed partial class FlatpackCreatorComponent : Component
{

}
