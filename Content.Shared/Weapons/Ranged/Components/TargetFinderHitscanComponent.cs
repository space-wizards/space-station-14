using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Makes so when this hitscan hits something, it marks them as the target for the gun that shot the hitscan
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TargetFinderHitscanComponent : Component;
