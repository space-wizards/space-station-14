// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.DeadSpace.LieDown;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LieDownComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DownDelay = TimeSpan.FromSeconds(1f);

    [DataField, AutoNetworkedField]
    public TimeSpan UpDelay = TimeSpan.FromSeconds(1.5f);

    [ViewVariables, AutoNetworkedField]
    public bool DrawDowned { get; set; } = false;

    [DataField, AutoNetworkedField]
    public float WalkSpeedModifier = 0.2f;

    [DataField, AutoNetworkedField]
    public float RunSpeedModifier = 0.2f;

    [AutoNetworkedField]
    public bool IsLieDown = false;
}
