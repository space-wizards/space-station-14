using Content.Shared.Atmos.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.MedicalScanner;
using Content.Shared.Tools;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
namespace Content.Shared.Medical.Cryogenics;

/// <summary>
/// Component for medical cryo pods.
/// Handles transferring reagents from a beaker slot into an inserted mob, as well as exposing them to connected atmos pipes.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CryoPodComponent : Component
{
    /// <summary>
    /// The name of the container the patient is stored in.
    /// </summary>
    public const string BodyContainerName = "scanner-body";

    /// <summary>
    /// The name of the solution container for the injection chamber.
    /// </summary>
    public const string InjectionBufferSolutionName = "injectionBuffer";

    /// <summary>
    /// Specifies the name of the atmospherics port to draw gas from.
    /// </summary>
    [DataField]
    public string PortName = "port";

    /// <summary>
    /// Specifies the name of the slot that holds the beaker with medicine.
    /// </summary>
    [DataField]
    public string SolutionContainerName = "beakerSlot";

    /// <summary>
    /// How often are chemicals transferred from the beaker to the body?
    /// (injection interval)
    /// </summary>
    [DataField]
    public TimeSpan BeakerTransferTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The timestamp for the next injection.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextInjectionTime = TimeSpan.Zero;


    /// <summary>
    /// How often the UI is updated.
    /// </summary>
    [DataField]
    public TimeSpan UiUpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The timestamp for the next UI update.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUiUpdateTime = TimeSpan.Zero;

    /// <summary>
    /// How many units to transfer per injection from the beaker to the mob?
    /// </summary>
    [DataField]
    public FixedPoint2 BeakerTransferAmount = 1;

    /// <summary>
    /// Delay applied when inserting a mob in the pod (in seconds).
    /// </summary>
    [DataField]
    public float EntryDelay = 2f;

    /// <summary>
    /// Delay applied when trying to pry open a locked pod (in seconds).
    /// </summary>
    [DataField]
    public float PryDelay = 5f;

    /// <summary>
    /// Container for mobs inserted in the pod.
    /// </summary>
    [ViewVariables]
    public ContainerSlot BodyContainer = default!;

    /// <summary>
    /// If true, the eject verb will not work on the pod and the user must use a crowbar to pry the pod open.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked;

    /// <summary>
    /// Causes the pod to be locked without being fixable by messing with wires.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PermaLocked;

    /// <summary>
    /// The tool quality needed to eject a body when the pod is locked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> UnlockToolQuality = "Prying";
}

[Serializable, NetSerializable]
public enum CryoPodVisuals : byte
{
    ContainsEntity,
    IsOn
}

[Serializable, NetSerializable]
public enum CryoPodUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CryoPodUserMessage : BoundUserInterfaceMessage
{
    public GasAnalyzerComponent.GasMixEntry GasMix;
    public HealthAnalyzerUiState Health;
    public FixedPoint2? BeakerCapacity;
    public List<ReagentQuantity>? Beaker;
    public List<ReagentQuantity>? Injecting;

    public CryoPodUserMessage(
        GasAnalyzerComponent.GasMixEntry gasMix,
        HealthAnalyzerUiState health,
        FixedPoint2? beakerCapacity,
        List<ReagentQuantity>? beaker,
        List<ReagentQuantity>? injecting)
    {
        GasMix = gasMix;
        Health = health;
        BeakerCapacity = beakerCapacity;
        Beaker = beaker;
        Injecting = injecting;
    }
}

[Serializable, NetSerializable]
public sealed class CryoPodSimpleUiMessage : BoundUserInterfaceMessage
{
    public enum MessageType { EjectPatient, EjectBeaker }

    public readonly MessageType Type;

    public CryoPodSimpleUiMessage(MessageType type)
    {
        Type = type;
    }
}

[Serializable, NetSerializable]
public sealed class CryoPodInjectUiMessage : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 Quantity;

    public CryoPodInjectUiMessage(FixedPoint2 quantity)
    {
        Quantity = quantity;
    }
}
