using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components;

/// <summary>
/// Invokes its port when dropped or if its user goes crit/dies.
/// </summary>
[RegisterComponent]
public sealed class DeadMansSwitchComponent : Component
{
    public bool Armed;

    /// <summary>
    ///     The port that gets signaled when the switch fires.
    /// </summary>
    [DataField("port", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
    public string Port = "Activated";
}
