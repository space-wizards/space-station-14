using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;

namespace Content.Server.NPC.Components;

[RegisterComponent]
public sealed class NPCJukeComponent : Component
{
    [DataField("jukeType")]
    public JukeType JukeType = JukeType.Away;

    /// <summary>
    /// Are we actively juking
    /// </summary>
    [DataField("juking")]
    public bool Juking = false;
}
