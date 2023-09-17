using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Implants.Components;

/// <summary>
/// Implants an entity automatically on MapInit.
/// </summary>
[RegisterComponent]
public sealed partial class AutoImplantComponent : Component
{
    /// <summary>
    /// List of implants to inject.
    /// </summary>
    [DataField("implants", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Implants = new();
}
