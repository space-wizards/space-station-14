using Content.Shared.Audio;
using Content.Shared.Smoking;
using Robust.Shared.GameStates;

namespace Content.Server._Impstation.FireStructure;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(FireStructureSystem))]
public sealed partial class FireStructureComponent : Component
{
    [DataField, AutoNetworkedField]
    public SmokableState CurrentState = SmokableState.Unlit;

    [DataField]
    public AmbientSoundComponentState? AmbientSound;

    [DataField]
    public PointLightComponentState? PointLight;
}
