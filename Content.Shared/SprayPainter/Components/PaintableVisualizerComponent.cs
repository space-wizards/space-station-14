namespace Content.Shared.SprayPainter.Components;

/// <summary>
/// Makes objects update their layer zero sprite whenever they are spraypainted.
/// Suitable for paintables with relatively simpler sprite setups (e.g. canisters, posters).
/// More complicated things like airlocks have their own individual systems.
/// </summary>
[RegisterComponent]
public sealed partial class PaintableVisualizerComponent : Component {}
