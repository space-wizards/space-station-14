namespace Content.Server.Xenoarchaeology.Equipment.Components;

/// <summary>
/// This is used for a machine that biases
/// an artifact placed on it to move up/down
/// </summary>
[RegisterComponent]
public sealed partial class TraversalDistorterComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public BiasDirection BiasDirection = BiasDirection.In;

    public TimeSpan NextActivation = default!;
    public TimeSpan ActivationDelay = TimeSpan.FromSeconds(1);
}

public enum BiasDirection : byte
{
    In, //down the tree, towards depth 0
    Out //up the tree, away from depth 0
}
