using Robust.Shared.GameStates;

namespace Content.Shared.Forensics.Components;

/// <summary>
/// This component stops the entity from leaving finger prints,
/// usually so fibers can be left instead.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DnaSubstanceTraceComponent : Component;
