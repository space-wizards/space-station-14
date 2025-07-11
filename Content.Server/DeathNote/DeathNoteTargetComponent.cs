namespace Content.Server.DeathNote;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class DeathNoteTargetComponent : Component
{
    public float KillDelay = 40f;

    public TimeSpan KillTime = TimeSpan.FromSeconds(40);

    public DeathNoteTargetComponent(float killDelay)
    {
        KillDelay = killDelay;
        KillTime = TimeSpan.FromSeconds(killDelay);
    }
}
