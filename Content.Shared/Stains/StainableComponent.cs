using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Stains;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public partial class StainableComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "stains";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Color StainColor = Color.White;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan SqueezeDuration = TimeSpan.FromSeconds(30);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SqueezeSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}
