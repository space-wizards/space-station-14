using Robust.Shared.Audio;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class MedipenRefillerComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan CompletionTime = TimeSpan.FromSeconds(10);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan RemainingTime = TimeSpan.Zero;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsActivated = false;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string? Result = "";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundPathSpecifier MachineNoise = new("/Audio/Machines/medipen_refiller_activated.ogg");

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string BufferSolutionName = "buffer";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string InputSlotName = "beakerSlot";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string MedipenSlotName = "medipenSlot";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string MedipenSolutionName = "pen";
}
