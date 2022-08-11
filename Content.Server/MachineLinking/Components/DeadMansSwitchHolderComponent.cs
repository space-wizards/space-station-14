namespace Content.Server.MachineLinking.Components;

/// <summary>
/// Added to people holding a dead man's switch.
/// </summary>
[RegisterComponent]
public sealed class DeadMansSwitchHolderComponent : Component
{
    public HashSet<DeadMansSwitchComponent> Switches = new();
}
