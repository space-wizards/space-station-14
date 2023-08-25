using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Server.Damage.Systems;

namespace Content.Server.Damage.Components;

/// <summary>
/// Signal that gets fired when the entity is hurt.
/// </summary>
[RegisterComponent, Access(typeof(DamageSignalSystem))]
public sealed partial class DamageSignalComponent : Component
{
    /// <summary>
    /// The port that will send the signal.
    /// </summary>
    [DataField("port", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<SourcePortPrototype>)), ViewVariables(VVAccess.ReadOnly)]
    public string Port = string.Empty;
}
