using Robust.Shared.GameStates;

namespace Content.Shared.Research.TechnologyDisk.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class DiskConsolePrintingComponent : Component
{
    [AutoNetworkedField]
    public TimeSpan FinishTime;
}
