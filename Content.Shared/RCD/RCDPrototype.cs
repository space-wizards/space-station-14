using Content.Shared.RCD.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.RCD;

/// <summary>
/// Contains the parameters for a RCD construction / operation
/// </summary>
[Prototype("rcd")]
public sealed class RCDPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name associated with the prototype
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("name", required: true)]
    public string SetName { get; private set; } = string.Empty;

    /// <summary>
    /// The category this prototype is filed under
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("category", required: true)]
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Texture path for this prototypes menu icon
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("texture", required: true)]
    public string TexturePath { get; private set; } = string.Empty;

    /// <summary>
    /// The RCD mode associated with the operation
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("mode", required: true)]
    public RcdMode Mode { get; private set; } = RcdMode.Invalid;

    /// <summary>
    /// The entity prototype that will be constructed (mode dependent)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("prototype")]
    public string? Prototype { get; private set; }

    /// <summary>
    /// Number of charges consumed when the operation is completed
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("cost")]
    public int Cost { get; private set; } = 1;

    /// <summary>
    /// The lenght of the operation 
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("delay")]
    public float Delay { get; private set; } = 1f;

    /// <summary>
    /// The visual effect that plays during this operation
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fx")]
    public string? Effect { get; private set; }
}
