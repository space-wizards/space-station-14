using Robust.Shared.GameStates;

namespace Content.Shared.KillTome;

/// <summary>
/// Entity with this component is a Kill Tome target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KillTomeTargetComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public float KillDelay = 40f;

    [AutoNetworkedField]
    public TimeSpan KillTime;

    public KillTomeTargetComponent(float killDelay, TimeSpan killTime)
    {
        KillDelay = killDelay;
        KillTime = killTime;
    }
}
