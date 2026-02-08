using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component ensures that solution containers retain DNA of reagents even after those reagents are taken out.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DnaSubstanceTraceComponent : Component;
