namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// Component used for allowing non-humans to be converted. (Mainly monkeys)
/// </summary>
[RegisterComponent, Access(typeof(SharedRevolutionarySystem))] // DS14: server-only marker; roster visibility is sent separately.
public sealed partial class AlwaysRevolutionaryConvertibleComponent : Component
{

}
