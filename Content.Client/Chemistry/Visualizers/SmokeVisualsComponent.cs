namespace Content.Client.Chemistry.Visualizers;

/// <summary>
/// A component that changes color to match its contained reagents.
/// Managed by <see cref="SmokeVisualizerSystem"/>.
/// Only functions with smoke at the moment.
/// </summary>
[RegisterComponent]
[Access(typeof(SmokeVisualizerSystem))]
public sealed class SmokeVisualsComponent : Component {}
