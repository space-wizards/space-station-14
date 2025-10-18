using Robust.Shared.GameStates;

namespace Content.Shared.Bed.Components;

/// <summary>
/// Tracking component added to entities buckled to stasis beds.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBedSystem))]
public sealed partial class StasisBedBuckledComponent : Component;
