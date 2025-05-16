using Content.Shared._Starlight.Medical.Limbs;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.Starlight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LimbWithItemsComponent : Component, IImplantable, IWithAction
{
    [DataField(readOnly: true, required: true), AutoNetworkedField]
    public List<EntProtoId> Items;

    [DataField, AutoNetworkedField]
    public List<EntityUid> ItemEntities = [];

    [DataField, AutoNetworkedField]
    public bool EntityIcon { get; set; } = false;

    [DataField, AutoNetworkedField]
    public EntProtoId Action { get; set; } = "ActionToggleCyberLimb";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity { get; set; }

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;

    [DataField, AutoNetworkedField]
    public bool Toggled;
}
