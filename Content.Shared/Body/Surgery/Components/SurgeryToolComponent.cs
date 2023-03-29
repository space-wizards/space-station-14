using Content.Shared.Body.Surgery.Operation.Step;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Body.Surgery.Components;

[RegisterComponent, NetworkedComponent]
public sealed class SurgeryToolComponent : Component
{
    /// <summary>
    /// Operation steps this tool can be used for.
    /// </summary>
    [DataField("steps", customTypeSerializer: typeof(PrototypeIdListSerializer<SurgeryStepPrototype>))]
    public List<string> Steps = new();

    /// <summary>
    /// DoAfter delay for using the tool in an operation.
    /// </summary>
    [DataField("delay"), ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 1f;
}

public record struct UseToolDoAfter;
