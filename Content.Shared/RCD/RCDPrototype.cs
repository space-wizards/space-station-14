using Content.Shared.Random;
using Content.Shared.RCD.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.RCD;

/// <summary>
/// Conatins the parameters for a RCD construction / operation
/// </summary>
[Prototype("rcd")]
public sealed class RCDPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    /// <summary>
    /// The name associated with the prototype
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("name")]
    public string SetName { get; private set; } = "???";

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
    /// The entity prototype to be constructed
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
    [ViewVariables(VVAccess.ReadWrite), DataField("visualFX")]
    public string? VisualEffect { get; private set; }

    /// <summary>
    /// The visual effect that plays when the operation is finished
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("finishFX")]
    public string? FinishEffect { get; private set; }
}
