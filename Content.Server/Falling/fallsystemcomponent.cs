namespace Content.Server.Falling;

[RegisterComponent]
public sealed partial class FallSystemComponent : Component
{
 public float MaxRandomRadius { get; set; } = 30.0f; // Decides the random teleport of the fallsystem
}
