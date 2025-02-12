using Content.Shared.Stunnable;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Stunnable;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedStunbatonSystem))]
public sealed partial class StunbatonComponent : Component
{
    [DataField, ViewVariables]
    [AutoNetworkedField]
    public float EnergyPerUse = 350;

    [DataField]
    public SoundSpecifier SparksSound = new SoundCollectionSpecifier("sparks");
}
