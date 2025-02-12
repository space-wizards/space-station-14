namespace Content.Server.Coordinates;

[RegisterComponent]
public sealed partial class SpawnRandomOffsetComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField] public float Offset = 0.5f;
}
