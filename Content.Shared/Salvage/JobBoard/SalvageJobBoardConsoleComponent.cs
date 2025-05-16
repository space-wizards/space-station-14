using Content.Shared.Cargo.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Salvage.JobBoard;

/// <summary>
/// Used to view the job board ui
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSalvageSystem))]
public sealed partial class SalvageJobBoardConsoleComponent : Component;

[Serializable, NetSerializable]
public sealed class SalvageJobBoardConsoleState : BoundUserInterfaceState
{
    public string Title;
    public float Progression;

    public List<ProtoId<CargoBountyPrototype>> AvailableJobs;

    public SalvageJobBoardConsoleState(string title, float progression, List<ProtoId<CargoBountyPrototype>> availableJobs)
    {
        Title = title;
        Progression = progression;
        AvailableJobs = availableJobs;
    }
}

[Serializable, NetSerializable]
public enum SalvageJobBoardUiKey : byte
{
    Key
}
