using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.Starlight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimbItemDeployerComponent  : Component, IWithAction
{
    [DataField, AutoNetworkedField]
    public bool EntityIcon { get; set; } = false;

    [DataField, AutoNetworkedField]
    public EntProtoId Action { get; set; } = "ActionToggleCyberLimb";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity { get; set; }

    [DataField]
    public SoundSpecifier? Sound;

    [DataField, AutoNetworkedField]
    public bool Toggled;

    [DataField, AutoNetworkedField]
    public string ContainerId = "cyberlimb";

    [DataField]
    public EntityWhitelist HandWhitelist;
}
