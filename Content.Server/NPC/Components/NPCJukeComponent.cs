using Content.Server.NPC.HTN.PrimitiveTasks.Operators.Combat;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.NPC.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class NPCJukeComponent : Component
{
    [DataField("jukeType")]
    public JukeType JukeType = JukeType.Away;

    [DataField("jukeDuration")]
    public float JukeDuration = 0.5f;

    [DataField("nextJuke", customTypeSerializer:typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextJuke;

    [DataField("targetTile")]
    public Vector2i? TargetTile;
}

public enum JukeType : byte
{
    /// <summary>
    /// Will move directly away from target if applicable.
    /// </summary>
    Away,

    /// <summary>
    /// Move to the adjacent tile for the specified duration.
    /// </summary>
    AdjacentTile
}
