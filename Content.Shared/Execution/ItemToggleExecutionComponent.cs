namespace Content.Shared.Execution;

/// <summary>
/// Can toggle the ability of a weapon to execute someone.
/// The esword uses this, you can't execute someone when it is retracted.
/// </summary>
[RegisterComponent]
public sealed partial class ItemToggleExecutionComponent: Component
{
}
