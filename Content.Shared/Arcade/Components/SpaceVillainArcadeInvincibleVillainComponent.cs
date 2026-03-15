using Content.Shared.Arcade.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Arcade.Components;

/// <summary>
///
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSpaceVillainArcadeSystem))]
public sealed partial class SpaceVillainArcadeInvincibleVillainComponent : Component;
