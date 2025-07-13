using Robust.Shared.GameStates;

namespace Content.Shared.DeathNote;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeathNoteTargetComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public float KillDelay = 40f;

    [AutoNetworkedField]
    public TimeSpan KillTime;

    public DeathNoteTargetComponent(float killDelay, TimeSpan killTime)
    {
        KillDelay = killDelay;
        KillTime = killTime;
    }
}
