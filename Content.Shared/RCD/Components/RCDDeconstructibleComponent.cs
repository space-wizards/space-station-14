using Content.Shared.RCD.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.RCD.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RCDSystem))]
public sealed partial class RCDDeconstructableComponent : Component
{
    /// <summary>
    /// Number of charges consumed when the deconstruction is completed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Cost = 1;

    /// <summary>
    /// The length of the deconstruction 
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 1f;

    /// <summary>
    /// The visual effect that plays during deconstruction
    /// </summary>
    [DataField("fx"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Effect = null;

    /// <summary>
    /// Toggles whether this entity is deconstructable or not
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Deconstructable = true;
}
