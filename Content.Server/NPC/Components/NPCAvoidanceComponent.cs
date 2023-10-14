namespace Content.Server.NPC.Components;

/// <summary>
/// Should this entity be considered for collision avoidance
/// </summary>
[RegisterComponent]
public sealed partial class NPCAvoidanceComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("enabled")]
    public bool Enabled = true;
}
