using Robust.Shared.GameStates;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Component used for allowing non-humans to be converted. (Mainly monkeys)
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedRevolutionarySystem))]
public sealed partial class AlwaysRevolutionaryConvertibleComponent : Component
{

}
