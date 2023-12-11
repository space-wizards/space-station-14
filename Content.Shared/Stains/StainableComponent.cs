using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Stains;

/// <summary>
/// Entities with this component can have stains that appear when solution is spilled on it
/// Uses solution to store the stains
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(SharedStainsSystem))]
public sealed partial class StainableComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "stains";

    /// <summary>
    /// Updated when solution changes to tint sprites
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Color StainColor = Color.White;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan SqueezeDuration = TimeSpan.FromSeconds(30);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SqueezeSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}
