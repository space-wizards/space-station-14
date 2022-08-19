using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components;

/// <summary>
/// For devices that send a signal when you let go or drop them (dead man's switches)
/// </summary>
[RegisterComponent]
public sealed class ReleaseSignallerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Armed;

    /// <summary>
    ///     The port that gets signaled when the switch fires.
    /// </summary>
    [DataField("port", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
    public string Port = "Pressed";
}
