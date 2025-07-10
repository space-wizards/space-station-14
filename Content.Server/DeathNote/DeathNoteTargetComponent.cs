namespace Content.Server.DeathNote;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class DeathNoteTargetComponent : Component
{
    public float KillDelay = 40f;

    public TimeSpan KillTime = TimeSpan.Zero;
}
