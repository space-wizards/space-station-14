using Content.Server.NPC.Systems;

namespace Content.Server.NPC.Components;

/// <summary>
/// Should this entity be considered for collision avoidance
/// </summary>
[RegisterComponent]
public sealed class NPCAvoidanceComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EnabledVV
    {
        get => Enabled;
        set => IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<NPCSteeringSystem>().SetEnabled(this, value);
    }

    [DataField("enabled")]
    public bool Enabled = true;
}
