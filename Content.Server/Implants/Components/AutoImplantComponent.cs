using Robust.Shared.Prototypes;

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
    [DataField(required: true)]
    public List<EntProtoId> Implants = new();
}
