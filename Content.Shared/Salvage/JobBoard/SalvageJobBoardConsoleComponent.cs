using Robust.Shared.GameStates;

namespace Content.Shared.Salvage.JobBoard;

[RegisterComponent, NetworkedComponent]
public sealed partial class SalvageJobBoardConsoleComponent : Component;

public enum SalvageJobBoardUiKey : byte
{
    Key
}
