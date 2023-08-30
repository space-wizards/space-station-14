namespace Content.Server.Morgue.Components;

/// <summary>
/// used to track actively cooking crematoriums
/// </summary>
[RegisterComponent]
public sealed partial class ActiveCrematoriumComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator = 0;
}
