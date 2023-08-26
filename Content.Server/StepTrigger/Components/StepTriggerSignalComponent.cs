using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.StepTrigger.Systems;

namespace Content.Server.StepTrigger.Components;

/// <summary>
/// Signal that gets fired when the entity slips another entity.
/// </summary>
[RegisterComponent, Access(typeof(StepTriggerSignalSystem))]
public sealed partial class StepTriggerSignalComponent : Component
{
    /// <summary>
    /// The port that will send the signal.
    /// </summary>
    [DataField("port", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>)), ViewVariables(VVAccess.ReadOnly)]
    public string Port = string.Empty;
}
