namespace Content.Server.Coordinates;

[RegisterComponent]
public sealed class SpawnRandomOffsetComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset")] public float Offset = 0.5f;
}
