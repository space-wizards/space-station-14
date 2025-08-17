using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

[RegisterComponent, NetworkedComponent]
[Access(typeof(StationAiFixerConsoleSystem))]
public sealed partial class StationAiFixerConsoleComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class StationAiFixerConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public StationAiFixerConsoleBoundUserInterfaceState()
    {

    }
}

[Serializable, NetSerializable]
public sealed class StationAiFixerConsoleMessage : BoundUserInterfaceMessage
{
    public StationAiFixerConsoleAction Action;

    public StationAiFixerConsoleMessage(StationAiFixerConsoleAction action)
    {
        Action = action;
    }
}

[Serializable, NetSerializable]
public enum StationAiFixerConsoleUiKey
{
    Key,
}

[Serializable, NetSerializable]
public enum StationAiFixerConsoleAction
{
    Eject,
    Repair,
    Purge,
}
