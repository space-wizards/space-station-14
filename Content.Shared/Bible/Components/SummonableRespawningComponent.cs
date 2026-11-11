using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// EntityQuery tracking component for summonables that are waiting for a respawn.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BibleSystem))]
public sealed partial class SummonableRespawningComponent : Component;
