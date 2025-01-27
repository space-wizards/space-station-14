using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// Can be used to define weapons capable of hitting thrown objects,
/// such as the baseball bat.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeflectThrownObjectsComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float DeflectSpeed = 25f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier DeflectSound = new SoundPathSpecifier("/Audio/_Impstation/Weapons/baseballbatdeflect.ogg");
}
