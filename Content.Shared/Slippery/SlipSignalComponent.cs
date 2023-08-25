using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Slippery;

/// <summary>
/// Signal that gets fired when the entity slips another entity.
/// </summary>
[RegisterComponent, Access(typeof(SlipperySystem))]
public sealed partial class SlipSignalComponent : Component
{
    /// <summary>
    /// The port that will send the signal.
    /// </summary>
    [DataField("port", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>)), ViewVariables(VVAccess.ReadOnly)]
    public string Port = string.Empty;
}
