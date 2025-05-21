using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage.JobBoard;

/// <summary>
/// Used to view the job board ui
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SalvageJobBoardConsoleComponent : Component
{
    /// <summary>
    /// A label that this computer can print out.
    /// </summary>
    [DataField]
    public EntProtoId LabelEntity = "PaperSalvageJobLabel";

    /// <summary>
    /// The sound made when printing occurs
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// The time at which the console will be able to print a label again.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextPrintTime = TimeSpan.Zero;

    /// <summary>
    /// The time between prints.
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);
}

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
public sealed class JobBoardPrintLabelMessage : BoundUserInterfaceMessage
{
    public string JobId;

    public JobBoardPrintLabelMessage(string jobId)
    {
        JobId = jobId;
    }
}

[Serializable, NetSerializable]
public enum SalvageJobBoardUiKey : byte
{
    Key
}
