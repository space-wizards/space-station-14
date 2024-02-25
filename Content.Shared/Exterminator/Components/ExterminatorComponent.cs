using Content.Shared.Exterminator.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Exterminator.Components;

/// <summary>
/// Main exterminator component.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedExterminatorSystem))]
public sealed partial class ExterminatorComponent : Component
{
}
