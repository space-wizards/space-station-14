namespace Content.Server.GreyStation.NPC;

/// <summary>
/// Makes this mob attack the user if they fail to pet it.
/// Requires <c>NPCRetaliation</c> and <c>InteractionPopup</c> to work.
/// </summary>
[RegisterComponent]
public sealed partial class HostileOnInteractFailComponent : Component;
