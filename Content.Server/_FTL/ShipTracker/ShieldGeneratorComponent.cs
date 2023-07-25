namespace Content.Server._FTL.ShipTracker;

/// <summary>
/// By having this on a grid, they get +1 shield capacity
/// </summary>
[RegisterComponent]
public sealed class ShieldGeneratorComponent : Component
{

}

/// <summary>
/// By having this on a grid, they get +1 shield amount
/// </summary>
[RegisterComponent]
public sealed class ActiveShieldGeneratorComponent : Component
{

}
