using Content.Shared.Beeper.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Beeper.Components;

/// <summary>
/// Beeps depending on proximity to target.
/// </summary>
/// <remarks>
/// Requires <see cref="BeeperComponent"/> and <see cref="ProximityDetectorComponent"/> to work.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ProximityBeeperSystem))]
public sealed partial class ProximityBeeperComponent : Component;
