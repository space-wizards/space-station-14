using Robust.Shared.GameStates;

namespace Content.Shared.NPC.Components;

/// <summary>
/// Prevents this mob from doing melee damage to friendly mobs.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoFriendlyFireComponent : Component;
